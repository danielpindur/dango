using Dango.ErrorHandling;
using Dango.Tests.Utils;

namespace Dango.Unit.Tests;

[TestFixture]
public partial class DangoGeneratorTests
{
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

        var result = GeneratorTestHelper.RunGenerator(source);

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
using Dango.Abstractions;

namespace Test.Namespace
{
    public class MyRegistrar : IDangoMapperRegistrar
    {
    }
}";

        var result = GeneratorTestHelper.RunGenerator(source);

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
using Dango.Abstractions;

namespace Test.Namespace
{
    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            // No mappings
        }
    }
}";

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Is.Empty);
    }
}