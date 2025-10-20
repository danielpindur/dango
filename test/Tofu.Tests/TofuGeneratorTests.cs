using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tofu.Tests;

[TestFixture]
public class TofuGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(string source, params string[] additionalSources)
    {
        var syntaxTrees = new[] { source }.Concat(additionalSources)
            .Select(s => CSharpSyntaxTree.ParseText(s))
            .ToArray();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(Abstractions.ITofuMapperRegistrar).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "Test.Assembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TofuGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out _,
            out _);

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

        Assert.That(result.Diagnostics.Single().Id, Is.EqualTo(DiagnosticDescriptors.MissingRegistrarInterfaceImplementation.Id));
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

        Assert.That(result.Diagnostics.Single().Id, Is.EqualTo(DiagnosticDescriptors.MissingRegisterMethod.Id));
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

    [Test]
    public void Generator_WithEnumMapping_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

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

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
        
        var generatedSource = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource, Does.Contain("namespace Test.Assembly.Generated.Tofu.Mappings;"));
        Assert.That(generatedSource, Does.Contain("public static class Test_Namespace_SourceEnumExtensions"));
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestinationEnum ToDestinationEnum(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }

    [Test]
    public void Generator_WithSingleSourceAndMultipleDestinationMappings_GeneratesSingleExtensionClassWithMultipleMappings()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestEnum1 { A, B }
    public enum DestEnum2 { A, B }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestEnum1>();
            registry.Enum<SourceEnum, DestEnum2>();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
        
        var generatedSource = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource, Does.Contain("public static class Test_Namespace_SourceEnumExtensions"));
        
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestEnum1 ToDestEnum1(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.A => Test.Namespace.DestEnum1.A"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.B => Test.Namespace.DestEnum1.B"));
        
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestEnum2 ToDestEnum2(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.A => Test.Namespace.DestEnum2.A"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.B => Test.Namespace.DestEnum2.B"));
        
        Assert.That(generatedSource, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }
    
    [Test]
    public void Generator_WithMultipleSourceMappings_GeneratesMultipleExtensionClasses()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum1 { A, B }
    public enum SourceEnum2 { A, B }
    
    public enum DestEnum { A, B }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum1, DestEnum>();
            registry.Enum<SourceEnum2, DestEnum>();
        }
    }
}";

        var result = RunGenerator(source);
        
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Length, Is.EqualTo(2));
        
        Assert.That(result.Results.Single().GeneratedSources[0].HintName, Is.EqualTo("Test_Namespace_SourceEnum1Extensions.g.cs"));
        Assert.That(result.Results.Single().GeneratedSources[1].HintName, Is.EqualTo("Test_Namespace_SourceEnum2Extensions.g.cs"));
        
        var generatedSource1 = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource1, Does.Contain("public static class Test_Namespace_SourceEnum1Extensions"));
        Assert.That(generatedSource1, Does.Contain("public static Test.Namespace.DestEnum ToDestEnum(this Test.Namespace.SourceEnum1 value)"));
        Assert.That(generatedSource1, Does.Contain("Test.Namespace.SourceEnum1.A => Test.Namespace.DestEnum.A"));
        Assert.That(generatedSource1, Does.Contain("Test.Namespace.SourceEnum1.B => Test.Namespace.DestEnum.B"));
        Assert.That(generatedSource1, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
        
        var generatedSource2 = result.GeneratedTrees[1].ToString();
        
        Assert.That(generatedSource2, Does.Contain("public static class Test_Namespace_SourceEnum2Extensions"));
        Assert.That(generatedSource2, Does.Contain("public static Test.Namespace.DestEnum ToDestEnum(this Test.Namespace.SourceEnum2 value)"));
        Assert.That(generatedSource2, Does.Contain("Test.Namespace.SourceEnum2.A => Test.Namespace.DestEnum.A"));
        Assert.That(generatedSource2, Does.Contain("Test.Namespace.SourceEnum2.B => Test.Namespace.DestEnum.B"));
        Assert.That(generatedSource2, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }
    
    [Test]
    public void Generator_WithMapByName_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestEnum { A, B }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestEnum>().MapByName();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
        
        var generatedSource = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource, Does.Contain("public static class Test_Namespace_SourceEnumExtensions"));
        
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestEnum ToDestEnum(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.A => Test.Namespace.DestEnum.A"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.B => Test.Namespace.DestEnum.B"));
        
        Assert.That(generatedSource, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }
    
    [Test]
    public void Generator_WithMapByValue_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestEnum { C, D }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestEnum>().MapByValue();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
        
        var generatedSource = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource, Does.Contain("public static class Test_Namespace_SourceEnumExtensions"));
        
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestEnum ToDestEnum(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.A => Test.Namespace.DestEnum.C"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.B => Test.Namespace.DestEnum.D"));
        
        Assert.That(generatedSource, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }
    
    [Test]
    public void Generator_WithDefault_GeneratesSingleExtensionClassWithMappings()
    {
        var source = @"
using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum { A, B }
    
    public enum DestEnum { A, C }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestEnum>().WithDefault(DestEnum.C);
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
        
        var generatedSource = result.GeneratedTrees[0].ToString();
        
        Assert.That(generatedSource, Does.Contain("public static class Test_Namespace_SourceEnumExtensions"));
        
        Assert.That(generatedSource, Does.Contain("public static Test.Namespace.DestEnum ToDestEnum(this Test.Namespace.SourceEnum value)"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.A => Test.Namespace.DestEnum.A"));
        Assert.That(generatedSource, Does.Contain("Test.Namespace.SourceEnum.B => Test.Namespace.DestEnum.C"));
        
        Assert.That(generatedSource, Does.Not.Contain("=> throw new System.ArgumentOutOfRangeException"));
    }
    
    [Test]
    public void Generator_WithDefaultMapByValue_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

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

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>().WithDefault(DestinationEnum.Value2).MapByValue();
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
    }
    
    [Test]
    public void Generator_WithMapByValueDefault_GeneratesExtensionClass()
    {
        var source = @"
using Tofu.Abstractions;

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

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>().MapByValue().WithDefault(DestinationEnum.Value2);
        }
    }
}";

        var result = RunGenerator(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Results.Single().GeneratedSources.Single().HintName, Is.EqualTo("Test_Namespace_SourceEnumExtensions.g.cs"));
    }
}