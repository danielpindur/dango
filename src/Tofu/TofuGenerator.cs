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

        var enumMappingsBySourceEnum = new Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>>(SymbolEqualityComparer.Default);
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
        
        GenerateSources(context, compilation, enumMappingsBySourceEnum);
    }

    private static Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> AnalyzeMappings(SourceProductionContext context, IMethodSymbol registerMethod, SemanticModel model)
    {
        var enumMappingsBySourceEnum = new Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>>(SymbolEqualityComparer.Default);
        var syntax = registerMethod.DeclaringSyntaxReferences.First().GetSyntax();
        var invocations = syntax.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var symbolInfo = model.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is IMethodSymbol { Name: "MapByValue" or "MapByName" } strategyMethod)
            {
                // Get parent enum to which this strategy belongs
                var parent = invocation.Parent;
                while (parent is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Expression is InvocationExpressionSyntax parentInvocation)
                    {
                        var parentSymbol = model.GetSymbolInfo(parentInvocation);
                        if (parentSymbol.Symbol is IMethodSymbol { Name: "Enum" } enumMethod)
                        {
                            var mappingStrategy = strategyMethod.Name == "MapByValue"
                                ? MappingStrategy.ByValue
                                : MappingStrategy.ByName;
                            
                            var enumPair = CreateEnumPair(context, parentInvocation, enumMethod);
                            
                            // Since we are parsing a tree the enum should have already been added to the dictionary
                            enumMappingsBySourceEnum[enumPair!.SourceEnum][enumPair].Strategy = mappingStrategy;
                            
                            break;
                        }
                    }
                    parent = parent.Parent;
                }
                continue;
            }

            if (symbolInfo.Symbol is IMethodSymbol { Name: "Enum" } method)
            {
                var enumPair = CreateEnumPair(context, invocation, method);
            
                if (enumPair != null)
                {
                    AddToResolvedMappings(
                        context,
                        invocation.GetLocation(),
                        enumMappingsBySourceEnum,
                        enumPair,
                        new EnumMapping());
                }
            }
        }
        
        return enumMappingsBySourceEnum;
    }
    
    private static EnumPair? CreateEnumPair(
        SourceProductionContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        if (method.TypeArguments.Length != 2)
        {
            return null;
        }

        if (method.TypeArguments[0] is not INamedTypeSymbol sourceEnum ||
            method.TypeArguments[1] is not INamedTypeSymbol destinationEnum)
        {
            return null;
        }

        if (sourceEnum.TypeKind != TypeKind.Enum)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidEnumType,
                    invocation.GetLocation(),
                    sourceEnum.Name));
            return null;
        }

        if (destinationEnum.TypeKind != TypeKind.Enum)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidEnumType,
                    invocation.GetLocation(),
                    destinationEnum.Name));
            return null;
        }
    
        return new EnumPair(sourceEnum, destinationEnum);
    }

    private static void AddToResolvedMappings(
        SourceProductionContext context, 
        Location location,
        Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> enumMappingsBySourceEnum,
        EnumPair enumPair,
        EnumMapping mapping)
    {
        if (enumMappingsBySourceEnum.ContainsKey(enumPair.SourceEnum))
        {
            if (enumMappingsBySourceEnum[enumPair.SourceEnum].ContainsKey(enumPair))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateEnumMapping,
                        location,
                        enumPair.SourceEnum.Name,
                        enumPair.DestinationEnum.Name));
            }
            else
            {
                enumMappingsBySourceEnum[enumPair.SourceEnum][enumPair] = mapping;
            }
        }
        else
        {
            enumMappingsBySourceEnum[enumPair.SourceEnum] = new Dictionary<EnumPair, EnumMapping>()
                { { enumPair, mapping } };
        }
    }

    private static void AppendMappings(
        SourceProductionContext context,
        Location location,
        Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> enumMappingsBySourceEnum,
        Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> mappingsToAddBySourceEnum)
    {
        foreach (var mappingsToAdd in mappingsToAddBySourceEnum)
        {
            foreach (var enumMapping in mappingsToAdd.Value)
            {
                AddToResolvedMappings(context, location, enumMappingsBySourceEnum, enumMapping.Key, enumMapping.Value);
            }
        }
    }
    
    private static void GenerateSources(
        SourceProductionContext context,
        Compilation compilation,
        Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> enumMappingsBySourceEnum)
    {
        var assemblyName = compilation.AssemblyName!;
        
        foreach (var enumMappingsWithSourceEnum in enumMappingsBySourceEnum)
        {
            var sourceEnum = enumMappingsWithSourceEnum.Key;
            
            var className = GenerateExtensionsClassName(sourceEnum);
            var generated = GenerateCode(className, assemblyName, enumMappingsWithSourceEnum.Value, context);
            
            context.AddSource($"{className}.g.cs", generated);
        }
    }

    private static string GenerateCode(
        string className,
        string assemblyName,
        IEnumerable<KeyValuePair<EnumPair, EnumMapping>> mappingsForEnumPairs,
        SourceProductionContext context)
    {
        // TODO: Change to use Roslyn syntax factory API
        // TODO: Map by value or by name
        // TODO: Overrides
        // TODO: Ignore
        // TODO: Default
        // TODO: Scoped namespace for compatibility reasons
        // TODO: If enums names are same, we should take first diff in namespace and use that in the method name
        // TODO: Nullable map
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Tofu tool. Do not change manually as changes will be overwritten.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine($"namespace {assemblyName}.Generated.Tofu.Mappings;");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        foreach (var mappingsForEnumPair in mappingsForEnumPairs)
        {
            var enumPair = mappingsForEnumPair.Key;
            var enumMapping = mappingsForEnumPair.Value;
            var sourceToDestinationValueMappings = ResolveValueMappings(context, enumPair, enumMapping);
            
            sb.AppendLine($$"""
                public static {{enumPair.DestinationEnum.ToDisplayString()}} To{{enumPair.DestinationEnum.Name}}(this {{enumPair.SourceEnum.ToDisplayString()}} value) =>
                    value switch
                    {
                    {{GenerateValueMappings(enumPair, sourceToDestinationValueMappings)}}
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
    
    private static IEnumerable<(string SourceValue, string DestinationValue)> ResolveValueMappings(
        SourceProductionContext context,
        EnumPair enumPair,
        EnumMapping mapping)
    {
        if (mapping.Strategy == MappingStrategy.ByValue)
        {
            return ResolveValueMappingsByValue(context, enumPair, mapping);
        }
        
        return ResolveValueMappingsByName(context, enumPair, mapping);
    }

    private static IEnumerable<(string SourceValue, string DestinationValue)> ResolveValueMappingsByName(
        SourceProductionContext context,
        EnumPair enumPair,
        EnumMapping mapping)
    {
        var sourceEnumValues = enumPair.SourceEnum.MemberNames;
        var destinationEnumValues = enumPair.DestinationEnum.MemberNames.ToDictionary(x => x);

        foreach (var enumValue in sourceEnumValues)
        {
            if (!destinationEnumValues.TryGetValue(enumValue!, out var destinationValue))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                        Location.None,
                        enumValue,
                        enumPair.SourceEnum.Name,
                        enumPair.DestinationEnum.Name));
                continue;
            }

            yield return (enumValue, destinationValue);
        }
    }

    private static IEnumerable<(string SourceValue, string DestinationValue)> ResolveValueMappingsByValue(
        SourceProductionContext context,
        EnumPair enumPair,
        EnumMapping mapping)
    {
        var sourceMembers = enumPair.SourceEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst)
            .ToDictionary(f => f.ConstantValue);

        var destMembers = enumPair.DestinationEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst)
            .ToDictionary(f => f.ConstantValue, f => f.Name);

        foreach (var sourceMember in sourceMembers)
        {
            if (!destMembers.TryGetValue(sourceMember.Key!, out var destName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                        Location.None,
                        sourceMember.Value.Name,
                        enumPair.SourceEnum.Name,
                        enumPair.DestinationEnum.Name));
                continue;
            }

            yield return (sourceMember.Value.Name, destName);
        }
    }
    
    private static string GenerateValueMappings(
        EnumPair enumPair,
        IEnumerable<(string sourceValue, string destinationValue)> sourceToDestinationMappings)
    {
        var sourceEnumName = enumPair.SourceEnum.ToDisplayString();
        var destinationEnumName = enumPair.DestinationEnum.ToDisplayString();

        var sb = new System.Text.StringBuilder();
        
        foreach (var sourceToDestinationMapping in sourceToDestinationMappings)
        {
            sb.AppendLine($"{sourceEnumName}.{sourceToDestinationMapping.sourceValue} => {destinationEnumName}.{sourceToDestinationMapping.destinationValue}");
        }
        
        return sb.ToString();
    }

    private class EnumPair : IEquatable<EnumPair>
    {
        private readonly int _hashCode;
        
        public EnumPair(
            INamedTypeSymbol sourceEnum,
            INamedTypeSymbol destinationEnum)
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
        
        public override bool Equals(object? obj)
        {
            if (obj is not EnumPair other)
            {
                return false;
            }
            
            return Equals(other);
        }

        public bool Equals(EnumPair other)
        {
            return other._hashCode == _hashCode;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
    
    private class EnumMapping
    {
        public MappingStrategy Strategy { get; set; }

        public HashSet<ValueMapping> Overrides { get; } = new();

        public HashSet<INamedTypeSymbol> IgnoredSourceValues { get; } = new(SymbolEqualityComparer.Default);
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