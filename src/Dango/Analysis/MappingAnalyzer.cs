using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Dango.ErrorHandling;
using Dango.Models;

namespace Dango.Analysis;

internal static class MappingAnalyzer
{
    public static Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> AnalyzeMappings(
        SourceProductionContext context,
        IMethodSymbol registerMethod,
        SemanticModel model)
    {
        var enumMappingsBySourceEnum =
            new Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>>(SymbolEqualityComparer.Default);

        var syntax = registerMethod.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;

        if (syntax?.Body is null)
        {
            return enumMappingsBySourceEnum;
        }

        var rootInvocations = syntax.Body.Statements.OfType<ExpressionStatementSyntax>()
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
                if (enumPair is null)
                {
                    var methodSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    enumPair = CreateEnumPair(context, rootInvocation, methodSymbol);
                    continue;
                }

                ProcessChainedInvocation(invocation, model, enumMapping);
            }

            if (enumPair is not null)
            {
                AddMapping(enumMappingsBySourceEnum, enumPair, enumMapping);
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
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidEnumType, invocation.GetLocation(), sourceEnum.Name));
            return null;
        }

        if (destinationEnum.TypeKind != TypeKind.Enum)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidEnumType, invocation.GetLocation(), destinationEnum.Name));
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

        return invocationChain;
    }

    private static void ProcessChainedInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        EnumMapping enumMapping)
    {
        var symbolInfo = model.GetSymbolInfo(invocation.Expression);
        var chainedSymbol = symbolInfo.Symbol as IMethodSymbol;

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

    private static string? ExtractDefaultValue(
        InvocationExpressionSyntax chainedInvocation,
        SemanticModel model)
    {
        if (chainedInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var argument = chainedInvocation.ArgumentList.Arguments[0].Expression;

        var argumentSymbol = model.GetSymbolInfo(argument).Symbol;

        if (argumentSymbol is IFieldSymbol { IsConst: true } fieldSymbol)
        {
            return fieldSymbol.Name;
        }

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

        if (dictionaryExpression is ObjectCreationExpressionSyntax { Initializer: { } initializer })
        {
            foreach (var expression in initializer.Expressions)
            {
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

    private static void AddMapping(
        Dictionary<INamedTypeSymbol, Dictionary<EnumPair, EnumMapping>> enumMappingsBySourceEnum,
        EnumPair enumPair,
        EnumMapping mapping)
    {
        if (enumMappingsBySourceEnum.TryGetValue(enumPair.SourceEnum, out var mappingsByEnumPair))
        {
            mappingsByEnumPair[enumPair] = mapping;
        }
        else
        {
            enumMappingsBySourceEnum[enumPair.SourceEnum] = new Dictionary<EnumPair, EnumMapping>() { { enumPair, mapping } };
        }
    }
}