namespace Dango.Tests;

public partial class DangoGeneratorTests
{
    [Test]
    public void Generator_WithMapByName_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestinationEnum { A, B }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>().MapByName();
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.A"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.B"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithMapByValue_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestinationEnum { C, D }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>().MapByValue();
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.C"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.D"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithDefault_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestinationEnum { A, C }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .WithDefault(DestinationEnum.C);
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.A"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.C"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithDefaultMapByValue_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        Value1,
        Value3
    }

    public enum DestinationEnum
    {
        Value2
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .WithDefault(DestinationEnum.Value2)
                .MapByValue();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs")
        );
    }

    [Test]
    public void Generator_WithMapByValueDefault_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        Value1,
        Value3
    }

    public enum DestinationEnum
    {
        Value2
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithDefault(DestinationEnum.Value2);
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs")
        );
    }

    [Test]
    public void Generator_WithMapByNameOverrides_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        A,
        B
    }

    public enum DestinationEnum
    {
        A,
        B
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithOverrides(new Dictionary<SourceEnum, DestinationEnum>
                {
                    { SourceEnum.A, DestinationEnum.B },
                });
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.B"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.B"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithMapByValueOverrides_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        A,
        B
    }

    public enum DestinationEnum
    {
        C,
        D
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithOverrides(new Dictionary<SourceEnum, DestinationEnum>
                {
                    { SourceEnum.A, DestinationEnum.D },
                });
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.D"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.D"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithMapByNameMultipleOverrides_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        A,
        B
    }

    public enum DestinationEnum
    {
        A,
        B
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithOverrides(new Dictionary<SourceEnum, DestinationEnum>
                {
                    { SourceEnum.A, DestinationEnum.B },
                    { SourceEnum.B, DestinationEnum.A },
                });
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.B"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.A"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithMapByValueMultipleOverrides_GeneratesExtensionClass()
    {
        var source = @"
using Dango.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        A,
        B
    }

    public enum DestinationEnum
    {
        C,
        D
    }

    public class MyRegistrar : IDangoMapperRegistrar
    {
        public void Register(IDangoMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithOverrides(new Dictionary<SourceEnum, DestinationEnum>
                {
                    { SourceEnum.A, DestinationEnum.D },
                    { SourceEnum.B, DestinationEnum.C },
                });
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
                "public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.A => Test.Namespace.DestinationEnum.D"
            )
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "Test.Namespace.SourceEnum.B => Test.Namespace.DestinationEnum.C"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }
}