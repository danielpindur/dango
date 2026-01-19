namespace Dango.Abstractions;

/// <summary>
/// Defines the configuration interface for mapping between two enum types.
/// </summary>
/// <typeparam name="TSource">The source enum type to map from.</typeparam>
/// <typeparam name="TDestination">The destination enum type to map to.</typeparam>
public interface IDangoEnumMappingConfiguration<TSource, TDestination>
    where TSource : struct, Enum
    where TDestination : struct, Enum
{
    /// <summary>
    /// Configures the mapping to match enum values by their underlying numeric values.
    /// For example, if SourceEnum.A = 0 and DestinationEnum.X = 0, they will be mapped together.
    /// </summary>
    /// <returns>The configuration instance for method chaining.</returns>
    IDangoEnumMappingConfiguration<TSource, TDestination> MapByValue();

    /// <summary>
    /// Configures the mapping to match enum values by their names (default behavior).
    /// For example, SourceEnum.Active will map to DestinationEnum.Active if both exist.
    /// </summary>
    /// <returns>The configuration instance for method chaining.</returns>
    IDangoEnumMappingConfiguration<TSource, TDestination> MapByName();

    /// <summary>
    /// Specifies a default destination value to use when a source enum value has no matching destination value.
    /// This prevents compilation errors when the destination enum has fewer values than the source.
    /// </summary>
    /// <param name="defaultValue">The default destination enum value to use for unmapped source values.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    IDangoEnumMappingConfiguration<TSource, TDestination> WithDefault(TDestination defaultValue);

    /// <summary>
    /// Provides custom mappings for specific source enum values, overriding the default mapping strategy.
    /// This is useful when certain values need special handling that doesn't follow the name or value pattern.
    /// </summary>
    /// <param name="overrides">A dictionary mapping source enum values to their corresponding destination values.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    IDangoEnumMappingConfiguration<TSource, TDestination> WithOverrides(
        IReadOnlyDictionary<TSource, TDestination> overrides);
}