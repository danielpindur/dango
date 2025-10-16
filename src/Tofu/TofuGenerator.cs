using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tofu.Abstractions;

namespace Tofu;

[Generator(LanguageNames.CSharp)]
public sealed class TofuGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(compilationAndCandidates, Execute);
    }

    private static void Execute(
        SourceProductionContext context,
        (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> candidates) input)
    {
        var (compilation, candidates) = input;
        var registrarInterface = compilation.GetTypeByMetadataName(typeof(ITofuMapperRegistrar).FullName!)!;

        var enumMappingsBySourceEnum = new Dictionary<INamedTypeSymbol, HashSet<EnumMapping>>(SymbolEqualityComparer.Default);
        var registrarInterfaceImplementationFound = false;
        
        foreach (var cls in candidates)
        {
            var model = compilation.GetSemanticModel(cls.SyntaxTree);
            if (model.GetDeclaredSymbol(cls) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            if (!symbol.AllInterfaces.Contains(registrarInterface, SymbolEqualityComparer.Default))
            {
                continue;
            }

            registrarInterfaceImplementationFound = true;

            var registerMethod = symbol.GetMembers(nameof(ITofuMapperRegistrar.Register))
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 1);

            if (registerMethod is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingRegisterMethod,
                        cls.Identifier.GetLocation(),
                        symbol.Name));
                continue;
            }

            var mappingInfo = AnalyzeMappings(context, registerMethod, model);
            AppendMappings(context, cls.Identifier.GetLocation(), enumMappingsBySourceEnum, mappingInfo);
        }

        if (!registrarInterfaceImplementationFound)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DiagnosticDescriptors.MissingRegistrarInterfaceImplementation, Location.None));
            return;
        }
        
        GenerateSources(context, enumMappingsBySourceEnum);
    }

    private static Dictionary<INamedTypeSymbol, HashSet<EnumMapping>> AnalyzeMappings(SourceProductionContext context, IMethodSymbol registerMethod, SemanticModel model)
    {
        var enumMappingsBySourceEnum = new Dictionary<INamedTypeSymbol, HashSet<EnumMapping>>(SymbolEqualityComparer.Default);
        var syntax = registerMethod.DeclaringSyntaxReferences.First().GetSyntax();
        var invocations = syntax.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var symbolInfo = model.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol { Name: "Enum" } method)
            {
                continue;
            }

            if (method.TypeArguments.Length != 2)
            {
                continue;
            }

            if (method.TypeArguments[0] is not INamedTypeSymbol sourceEnum ||
                method.TypeArguments[1] is not INamedTypeSymbol destinationEnum)
            {
                continue;
            }

            if (sourceEnum.TypeKind != TypeKind.Enum)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidEnumType,
                        invocation.GetLocation(),
                        sourceEnum.Name));
                continue;
            }

            if (destinationEnum.TypeKind != TypeKind.Enum)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidEnumType,
                        invocation.GetLocation(),
                        destinationEnum.Name));
                continue;
            }
            
            var mapping = new EnumMapping(sourceEnum, destinationEnum);
            AddToResolvedMappings(context, invocation.GetLocation(), enumMappingsBySourceEnum, mapping);
        }
        
        return enumMappingsBySourceEnum;
    }

    private static void AddToResolvedMappings(
        SourceProductionContext context, 
        Location location,
        Dictionary<INamedTypeSymbol, HashSet<EnumMapping>> enumMappingsBySourceEnum,
        EnumMapping mapping)
    {
        if (enumMappingsBySourceEnum.ContainsKey(mapping.SourceEnum))
        {
            if (!enumMappingsBySourceEnum[mapping.SourceEnum].Add(mapping))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateEnumMapping,
                        location,
                        mapping.SourceEnum.Name,
                        mapping.DestinationEnum.Name));
            }
        }
        else
        {
            enumMappingsBySourceEnum[mapping.SourceEnum] = [mapping];
        }
    }

    private static void AppendMappings(
        SourceProductionContext context,
        Location location,
        Dictionary<INamedTypeSymbol, HashSet<EnumMapping>> enumMappingsBySourceEnum,
        Dictionary<INamedTypeSymbol, HashSet<EnumMapping>> mappingsToAddBySourceEnum)
    {
        foreach (var mappingsToAdd in mappingsToAddBySourceEnum)
        {
            foreach (var enumMapping in mappingsToAdd.Value)
            {
                AddToResolvedMappings(context, location, enumMappingsBySourceEnum, enumMapping);
            }
        }
    }
    
    private static void GenerateSources(
        SourceProductionContext context, 
        Dictionary<INamedTypeSymbol, HashSet<EnumMapping>> enumMappingsBySourceEnum)
    {
        foreach (var enumMappingsWithSourceEnum in enumMappingsBySourceEnum)
        {
            var sourceEnum = enumMappingsWithSourceEnum.Key;
            
            var className = GenerateExtensionsClassName(sourceEnum);
            var generated = GenerateCode(className, enumMappingsWithSourceEnum.Value);
            
            
            context.AddSource($"{className}.g.cs", generated);
        }
    }

    private static string GenerateCode(string className, IEnumerable<EnumMapping> enumMappings)
    {
        // TODO: Change to use Roslyn syntax factory API
        // TODO: Actual mapping
        // TODO: Map by value or by name
        // TODO: Overrides
        // TODO: Ignore
        // TODO: Default
        // TODO: Scoped namespace for compatibility reasons
        // TODO: If enums names are same, we should take first diff in namespace and use that in the method name
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Tofu tool. Do not change manually as changes will be overwritten.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("namespace Tofu.Generated;"); // TODO: Namespace needs to be the assembly where it was registered
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        foreach (var mapping in enumMappings)
        {
            sb.AppendLine($$"""
                public static {{mapping.DestinationEnum.ToDisplayString()}} To{{mapping.DestinationEnum.Name}}(this {{mapping.SourceEnum.ToDisplayString()}} value) =>
                    value switch
                    {
                    };
                """);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
    
    private static string GenerateExtensionsClassName(INamedTypeSymbol sourceEnum)
    {
        var enumNamespace = sourceEnum.ContainingNamespace.ToDisplayString();
        var enumAssembly = sourceEnum.ContainingAssembly.ToDisplayString();

        var namePrefix = string.Empty;

        if (enumNamespace.Length > 0)
        {
            namePrefix = enumNamespace.Replace('.', '_');
        }
        else if (enumAssembly.Length > 0)
        {
            namePrefix = enumAssembly.Replace('.', '_');
        }

        return namePrefix.Length == 0 ? $"{sourceEnum.Name}Extensions" : $"{namePrefix}_{sourceEnum.Name}Extensions";
    }

    private class EnumMapping : IEquatable<EnumMapping>
    {
        private readonly int _hashCode;
        
        public EnumMapping(INamedTypeSymbol sourceEnum, INamedTypeSymbol destinationEnum)
        {
            SourceEnum = sourceEnum;
            DestinationEnum = destinationEnum;

            unchecked
            {
                _hashCode = StringComparer.Ordinal.GetHashCode(SourceEnum.ToDisplayString()) *
                            StringComparer.Ordinal.GetHashCode(DestinationEnum.ToDisplayString());
            }
        }

        public INamedTypeSymbol SourceEnum { get; }
    
        public INamedTypeSymbol DestinationEnum { get; }

        public HashSet<ValueMapping> Overrides { get; } = new();

        public override bool Equals(object? obj)
        {
            if (obj is not EnumMapping other)
            {
                return false;
            }
            
            return Equals(other);
        }

        public bool Equals(EnumMapping other)
        {
            return other._hashCode == _hashCode;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }

    private class ValueMapping : IEquatable<ValueMapping>
    {
        private readonly int _hashCode;
        
        public ValueMapping(string sourceValue, string destinationValue)
        {
            SourceValue = sourceValue;
            DestinationValue = destinationValue;
            
            _hashCode = StringComparer.Ordinal.GetHashCode(SourceValue);
        }

        public string SourceValue { get; }
    
        public string DestinationValue { get; }

        public override bool Equals(object? obj)
        {
            if (obj is not ValueMapping other)
            {
                return false;
            } 
            
            return Equals(other);
        }

        public bool Equals(ValueMapping other)
        {
            return other._hashCode == _hashCode;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}