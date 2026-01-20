using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dango.Tests.Utils;

public static class GeneratorTestHelper
{
    public static Compilation CreateCompilation(string source, params string[] additionalSources)
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
                typeof(Abstractions.IDangoMapperRegistrar).Assembly.Location
            )
        );

        return CSharpCompilation.Create(
            "Test.Assembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    public static GeneratorDriverRunResult RunGenerator(string source, params string[] additionalSources)
    {
        var compilation = CreateCompilation(source, additionalSources);
        var generator = new DangoGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver = (CSharpGeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult();
    }
}
