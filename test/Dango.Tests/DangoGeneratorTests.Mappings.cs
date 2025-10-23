namespace Dango.Tests;

public partial class DangoGeneratorTests
{
    [Test]
    public void Generator_WithEnumMapping_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        Value1,
        Value2
    }

    public enum DestinationEnum
    {
        Value1,
        Value2
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs")
        );

        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource,
            Does.Contain("namespace Test.Assembly.Generated.Dango.Mappings")
        );
        Assert.That(
            generatedSource,
            Does.Not.Contain("namespace Test.Assembly.Generated.Dango.Mappings;")
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "public static class Test_Namespace_SourceEnumExtensions"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum? ToDestinationEnum(this Test.Namespace.SourceEnum? value)"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithSingleSourceAndMultipleDestinationMappings_GeneratesSingleExtensionClassWithMultipleMappings()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestinationEnum1 { A, B }
    public enum DestinationEnum2 { A, B }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum1>();
            registry.Enum<SourceEnum, DestinationEnum2>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs")
        );

        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static class Test_Namespace_SourceEnumExtensions"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum1 ToDestinationEnum1(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum1.A"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum1.B"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum2 ToDestinationEnum2(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum2.A"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum2.B"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithMultipleSourceMappings_GeneratesMultipleExtensionClasses()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum1 { A, B }
    public enum SourceEnum2 { A, B }
    
    public enum DestinationEnum { A, B }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum1, DestinationEnum>();
            registry.Enum<SourceEnum2, DestinationEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Length,
            Is.EqualTo(2)
        );

        Assert.That(
            result.Results.Single().GeneratedSources[0].HintName,
            Is.EqualTo("Test_Namespace_SourceEnum1Extensions.g.cs")
        );
        Assert.That(
            result.Results.Single().GeneratedSources[1].HintName,
            Is.EqualTo("Test_Namespace_SourceEnum2Extensions.g.cs")
        );

        var generatedSource1 = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource1,
            Does.Contain(
                "public static class Test_Namespace_SourceEnum1Extensions"
            )
        );
        Assert.That(
            generatedSource1,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum1 value)"
            )
        );
        Assert.That(
            generatedSource1,
            Does.Contain(
                "Test.Namespace.SourceEnum1.A => Test.Namespace.DestinationEnum.A"
            )
        );
        Assert.That(
            generatedSource1,
            Does.Contain(
                "Test.Namespace.SourceEnum1.B => Test.Namespace.DestinationEnum.B"
            )
        );
        Assert.That(
            generatedSource1,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );

        var generatedSource2 = result.GeneratedTrees[1].ToString();

        Assert.That(
            generatedSource2,
            Does.Contain(
                "public static class Test_Namespace_SourceEnum2Extensions"
            )
        );
        Assert.That(
            generatedSource2,
            Does.Contain(
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum2 value)"
            )
        );
        Assert.That(
            generatedSource2,
            Does.Contain(
                "Test.Namespace.SourceEnum2.A => Test.Namespace.DestinationEnum.A"
            )
        );
        Assert.That(
            generatedSource2,
            Does.Contain(
                "Test.Namespace.SourceEnum2.B => Test.Namespace.DestinationEnum.B"
            )
        );
        Assert.That(
            generatedSource2,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }
}