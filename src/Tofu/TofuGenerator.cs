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
        var syntax = registerMethod.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;

        if (syntax?.Body is null)
        {
            return enumMappingsBySourceEnum;
        }
        
        var rootInvocations = syntax.Body
            .Statements
            .OfType<ExpressionStatementSyntax>()
            .Select(stmt => stmt.Expression)
            .OfType<InvocationExpressionSyntax>()
            .ToList();
        
        foreach (var rootInvocation in rootInvocations)
        {
            var invocationChain = GetInvocationChain(rootInvocation);

            EnumPair? enumPair = null;
            var enumMapping = new EnumMapping();
            
            foreach (var invocation in invocationChain)
            {
                // If we don't have an enum pair set, try to set it based on the current invocation
                if (enumPair is null)
                {
                    var methodSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    enumPair = CreateEnumPair(context, rootInvocation, methodSymbol);
                    continue;
                }
                
                // If we already have an enum pair, then this is chained method invocation
                var symbolInfo = model.GetSymbolInfo(invocation.Expression);
                var chainedSymbol = symbolInfo.Symbol as IMethodSymbol;

                // Fallback to candidate symbols if primary resolution fails
                if (chainedSymbol is null && symbolInfo.CandidateSymbols.Length > 0)
                {
                    chainedSymbol = symbolInfo.CandidateSymbols[0] as IMethodSymbol;
                }
                
                switch (chainedSymbol?.Name)
                {
                    case "MapByValue":
                        enumMapping.Strategy = MappingStrategy.ByValue;
                        break;

                    case "MapByName":
                        enumMapping.Strategy = MappingStrategy.ByName;
                        break;

                    case "WithDefault":
                        enumMapping.DefaultValue = ExtractDefaultValue(invocation, model);
                        break;

                    case "WithOverrides":
                        enumMapping.Overrides = ExtractOverrides(invocation, model);
                        break;
                }
            }

            if (enumPair is not null)
            {
                AddToResolvedMappings(
                    context,
                    rootInvocation.GetLocation(),
                    enumMappingsBySourceEnum,
                    enumPair,
                    enumMapping);
            }
        }
        
        return enumMappingsBySourceEnum;
    }
    
    private static EnumPair? CreateEnumPair(
        SourceProductionContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol? methodSymbol)
    {
        if (methodSymbol?.Name != "Enum")
        {
            return null;
        }
        
        if (methodSymbol.TypeArguments.Length != 2)
        {
            return null;
        }

        if (methodSymbol.TypeArguments[0] is not INamedTypeSymbol sourceEnum ||
            methodSymbol.TypeArguments[1] is not INamedTypeSymbol destinationEnum)
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
    
    private static IEnumerable<InvocationExpressionSyntax> GetInvocationChain(InvocationExpressionSyntax invocation)
    {
        var invocationChain = new Stack<InvocationExpressionSyntax>();
        var current = invocation;

        while (current is not null)
        {
            invocationChain.Push(current);
            
            if (current.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax inner })
            {
                current = inner;
            }
            else
            {
                current = null;
            }
        }

        return invocationChain; // Innermost first: Enum → WithDefault → MapByValue
    }

    
    private static string? ExtractDefaultValue(InvocationExpressionSyntax chainedInvocation, SemanticModel model)
    {
        if (chainedInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var argument = chainedInvocation.ArgumentList.Arguments[0].Expression;

        // Try to resolve the argument symbol (e.g. DestinationEnum.Value2)
        var argumentSymbol = model.GetSymbolInfo(argument).Symbol;

        if (argumentSymbol is IFieldSymbol { IsConst: true } fieldSymbol)
        {
            return fieldSymbol.Name;
        }

        // Fallback: try semantic constant value resolution
        var constantValue = model.GetConstantValue(argument);
        if (constantValue.HasValue)
        {
            return constantValue.Value?.ToString();
        }

        return null;
    }

    private static Dictionary<string, string>? ExtractOverrides(
        InvocationExpressionSyntax invocation,
        SemanticModel model)
    {
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return null;
        }
        
        var overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        
        var dictionaryExpression = invocation.ArgumentList.Arguments[0].Expression;

        // We're looking for: new Dictionary<SourceEnum, DestinationEnum> { { A, B }, { C, D } }
        if (dictionaryExpression is ObjectCreationExpressionSyntax { Initializer: { } initializer })
        {
            foreach (var expression in initializer.Expressions)
            {
                // Each initializer should look like { key, value }
                if (expression is InitializerExpressionSyntax { Expressions.Count: 2 } kvp)
                {
                    var sourceValueExpression = kvp.Expressions[0];
                    var destinationValueExpression = kvp.Expressions[1];

                    var sourceValueSymbol = model.GetSymbolInfo(sourceValueExpression).Symbol as IFieldSymbol;
                    var destinationValueSymbol = model.GetSymbolInfo(destinationValueExpression).Symbol as IFieldSymbol;

                    if (sourceValueSymbol is { IsConst: true } && destinationValueSymbol is { IsConst: true })
                    {
                        overrides.Add(sourceValueSymbol.Name, destinationValueSymbol.Name);
                    }
                }
            }
        }

        return overrides;
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
            if (mapping.Overrides is not null &&
                mapping.Overrides.TryGetValue(enumValue, out var destNameOverride))
            {
                yield return (enumValue, destNameOverride!);
            }
            else if (destinationEnumValues.TryGetValue(enumValue!, out var destinationValue))
            {
                yield return (enumValue, destinationValue);
            }
            else if (mapping.DefaultValue is not null)
            {
                yield return (enumValue, mapping.DefaultValue);
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                        Location.None,
                        enumValue,
                        enumPair.SourceEnum.Name,
                        enumPair.DestinationEnum.Name));
            }
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
            if (mapping.Overrides is not null &&
                mapping.Overrides.TryGetValue(sourceMember.Value.Name, out var destNameOverride))
            {
                yield return (sourceMember.Value.Name, destNameOverride!);
            }
            if (destMembers.TryGetValue(sourceMember.Key!, out var destName))
            {
                yield return (sourceMember.Value.Name, destName);
            }
            else if (mapping.DefaultValue is not null)
            {
                yield return (sourceMember.Value.Name, mapping.DefaultValue);
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                        Location.None,
                        sourceMember.Value.Name,
                        enumPair.SourceEnum.Name,
                        enumPair.DestinationEnum.Name));
            }
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

        public string? DefaultValue { get; set; }
        
        public Dictionary<string, string>? Overrides { get; set; }
    }
}