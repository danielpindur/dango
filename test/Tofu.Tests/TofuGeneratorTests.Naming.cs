namespace Tofu.Tests;

public partial class TofuGeneratorTests
{
    [Test]
    public void Generator_WithSameEnumNames_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Api 
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test.Service.Api 
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test
{
    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<Api.TestEnum, Service.Api.TestEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Api_TestEnumExtensions.g.cs")
        );

        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource,
            Does.Contain("namespace Test.Assembly.Generated.Tofu.Mappings")
        );
        Assert.That(
            generatedSource,
            Does.Contain("public static class Test_Api_TestEnumExtensions")
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Service.Api.TestEnum ToServiceTestEnum(this Test.Api.TestEnum value)"
            )
        );

        Assert.That(
            generatedSource,
            Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithSameEnumNamesSourcePrefix_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Service 
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test.Service.Api 
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test
{
    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<Service.TestEnum, Service.Api.TestEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Service_TestEnumExtensions.g.cs")
        );

        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource,
            Does.Contain("namespace Test.Assembly.Generated.Tofu.Mappings")
        );
        Assert.That(
            generatedSource,
            Does.Contain("public static class Test_Service_TestEnumExtensions")
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Service.Api.TestEnum ToApiTestEnum(this Test.Service.TestEnum value)"
            )
        );

        Assert.That(
            generatedSource,
            Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }

    [Test]
    public void Generator_WithSameEnumNamesDestinationPrefix_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Service.Api
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test.Service 
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}

namespace Test
{
    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<Service.Api.TestEnum, Service.TestEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(
            result.Results.Single().GeneratedSources.Single().HintName,
            Is.EqualTo("Test_Service_Api_TestEnumExtensions.g.cs")
        );

        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.That(
            generatedSource,
            Does.Contain("namespace Test.Assembly.Generated.Tofu.Mappings")
        );
        Assert.That(
            generatedSource,
            Does.Contain(
                "public static class Test_Service_Api_TestEnumExtensions"
            )
        );

        Assert.That(
            generatedSource,
            Does.Contain(
                "public static Test.Service.TestEnum ToTestEnum(this Test.Service.Api.TestEnum value)"
            )
        );

        Assert.That(
            generatedSource,
            Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException")
        );
    }
}