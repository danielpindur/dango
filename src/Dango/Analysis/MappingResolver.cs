using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Dango.ErrorHandling;
using Dango.Models;

namespace Dango.Analysis;

internal static class MappingResolver
{
    public static IEnumerable<(string SourceValue, string DestinationValue)> ResolveValueMappings(
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
        var sourceEnumValues = enumPair.SourceEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst && f.IsStatic)
            .Select(f => f.Name)
            .ToList();

        var destinationEnumValues = enumPair.DestinationEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst && f.IsStatic)
            .Select(f => f.Name)
            .ToImmutableHashSet();

        foreach (var enumValue in sourceEnumValues)
        {
            if (mapping.Overrides is not null && mapping.Overrides.TryGetValue(enumValue, out var destNameOverride))
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
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                    Location.None, enumValue, enumPair.SourceEnum.Name, enumPair.DestinationEnum.Name));
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
            .Where(f => f.IsConst && f.IsStatic)
            .ToDictionary(f => f.ConstantValue);

        var destMembers = enumPair.DestinationEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst && f.IsStatic)
            .ToDictionary(f => f.ConstantValue, f => f.Name);

        foreach (var sourceMember in sourceMembers)
        {
            if (mapping.Overrides is not null &&
                mapping.Overrides.TryGetValue(sourceMember.Value.Name, out var destNameOverride))
            {
                yield return (sourceMember.Value.Name, destNameOverride!);
            }
            else if (destMembers.TryGetValue(sourceMember.Key!, out var destName))
            {
                yield return (sourceMember.Value.Name, destName);
            }
            else if (mapping.DefaultValue is not null)
            {
                yield return (sourceMember.Value.Name, mapping.DefaultValue);
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingMappingForSourceEnumValue,
                    Location.None, sourceMember.Value.Name, enumPair.SourceEnum.Name, enumPair.DestinationEnum.Name));
            }
        }
    }
}