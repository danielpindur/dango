namespace Dango;

/// <summary>
/// Constants for well-known type metadata names.
/// These use nameof() to get compile-time type safety in the generator project,
/// but are used as strings at runtime (no runtime type loading required).
/// </summary>
internal static class WellKnownTypes
{
    // Compile-time constants that give us type safety
    public const string IDangoMapperRegistrarFullName = nameof(Dango) + "." + nameof(Abstractions) + "." + nameof(Abstractions.IDangoMapperRegistrar);
    public const string RegisterMethodName = nameof(Abstractions.IDangoMapperRegistrar.Register);
}
