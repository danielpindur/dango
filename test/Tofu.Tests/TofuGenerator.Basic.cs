using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tofu.ErrorHandling;

namespace Tofu.Tests;

[TestFixture]
public partial class TofuGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(
        string source,
        params string[] additionalSources
    )
    {
        var syntaxTrees = new[] { source }
            .Concat(additionalSources)
            .Select(s => CSharpSyntaxTree.ParseText(s))
            .ToArray();

        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Abstractions.ITofuMapperRegistrar).Assembly.Location
            )
        );

        var compilation = CSharpCompilation.Create(
            "Test.Assembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new TofuGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver = (CSharpGeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult();
    }

    [Test]
    public void Generator_WithNoRegistrars_GeneratesNoOutputWithWarning()
    {
        var source = @"
namespace Test.Namespace
{
    public class MyClass
    {
    }
}";

        var result = RunGenerator(source);

        Assert.That(
            result.Diagnostics.Single().Id,
            Is.EqualTo(
                DiagnosticDescriptors
                    .MissingRegistrarInterfaceImplementation
                    .Id
            )
        );
        Assert.That(result.GeneratedTrees.Length, Is.EqualTo(0));
    }

    [Test]
    public void Generator_WithRegistrarButInvalidRegisterMethod_GeneratesNoOutputWithError()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public class MyRegistrar : ITofuMapperRegistrar
    {
    }
}";

        var result = RunGenerator(source);

        Assert.That(
            result.Diagnostics.Single().Id,
            Is.EqualTo(DiagnosticDescriptors.MissingRegisterMethod.Id)
        );
        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public void Generator_WithRegistrarButNoEnumMappings_GeneratesNoOutput()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            // No mappings
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Is.Empty);
    }
}