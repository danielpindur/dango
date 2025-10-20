using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Dango.Abstractions;
using Dango.Analysis;
using Dango.CodeGeneration;
using Dango.ErrorHandling;
using Dango.Models;

namespace Dango;

[Generator(LanguageNames.CSharp)]
public sealed class DangoGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(compilationAndCandidates, Execute);
    }

    private static void Execute(SourceProductionContext context,
        (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> candidates) input)
    {
        var (compilation, candidates) = input;
        var registrarInterface = compilation.GetTypeByMetadataName(typeof(IDangoMapperRegistrar).FullName!)!;

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

            var registerMethod = symbol.GetMembers(nameof(IDangoMapperRegistrar.Register))
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 1);

            if (registerMethod is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingRegisterMethod,
                    cls.Identifier.GetLocation(), symbol.Name));
                continue;
            }

            var mappingInfo = MappingAnalyzer.AnalyzeMappings(context, registerMethod, model);
            AppendMappings(context, cls.Identifier.GetLocation(), enumMappingsBySourceEnum, mappingInfo);
        }

        if (!registrarInterfaceImplementationFound)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingRegistrarInterfaceImplementation,
                Location.None));
            return;
        }

        CodeGenerator.GenerateSources(context, compilation, enumMappingsBySourceEnum);
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
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateEnumMapping, location,
                    enumPair.SourceEnum.Name, enumPair.DestinationEnum.Name));
            }
            else
            {
                enumMappingsBySourceEnum[enumPair.SourceEnum][enumPair] = mapping;
            }
        }
        else
        {
            enumMappingsBySourceEnum[enumPair.SourceEnum] = new Dictionary<EnumPair, EnumMapping>() { { enumPair, mapping } };
        }
    }
}