using Microsoft.CodeAnalysis;

namespace Tofu.ErrorHandling;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MissingRegistrarInterfaceImplementation = new(
        id: "TOFU001",
        title: "Missing ITofuMapperRegistrar implementation",
        messageFormat: "Could not find any class implementing ITofuMapperRegistrar",
        category: "Tofu.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An implementation of ITofuMapperRegistrar interface is required for enum mapping generation.");

    public static readonly DiagnosticDescriptor MissingRegisterMethod = new(
        id: "TOFU002",
        title: "Missing Register method",
        messageFormat: "Class '{0}' implements ITofuMapperRegistrar but has no valid Register method",
        category: "Tofu.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ITofuMapperRegistrar implementations must have a Register method with one parameter.");

    public static readonly DiagnosticDescriptor DuplicateEnumMapping = new(
        id: "TOFU003",
        title: "Duplicate enum mapping",
        messageFormat: "Duplicate mapping from '{0}' to '{1}' detected",
        category: "Tofu.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple mappings for the same source and destination enum pair were found.");

    public static readonly DiagnosticDescriptor InvalidEnumType = new(
        id: "TOFU004",
        title: "Invalid enum type",
        messageFormat: "Type argument '{0}' in Enum<TSource, TDest>() is not an enum type",
        category: "Tofu.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Both type arguments to the Enum method must be enum types.");

    public static readonly DiagnosticDescriptor MissingMappingForSourceEnumValue = new(
        id: "TOFU005",
        title: "Missing mapping for source enum value",
        messageFormat: "Value '{0}' from enum {1} could not be mapped to a value from enum {2}",
        category: "Tofu.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every value, if not ignored, from source enum {1} must be mapped to a value from destination enum {2}.");
}