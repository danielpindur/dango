namespace Dango.Abstractions;

/// <summary>
/// Provides methods to register enum mappings for code generation.
/// This registry is used within the <see cref="IDangoMapperRegistrar.Register"/> method to configure mappings.
/// </summary>
public interface IDangoMapperRegistry
{
    /// <summary>
    /// Registers a mapping between two enum types and returns a configuration object for customization.
    /// By default, values are mapped by name. Use the returned configuration to specify different behavior.
    /// </summary>
    /// <typeparam name="TSource">The source enum type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination enum type to map to.</typeparam>
    /// <returns>A configuration object that allows customization of the mapping behavior.</returns>
    /// <example>
    /// <code>
    /// registry.Enum&lt;SourceStatus, DestinationStatus&gt;();
    /// registry.Enum&lt;SourcePriority, DestinationPriority&gt;().MapByValue();
    /// registry.Enum&lt;SourceState, DestinationState&gt;().WithDefault(DestinationState.Unknown);
    /// </code>
    /// </example>
    IDangoEnumMappingConfiguration<TSource, TDestination> Enum<TSource, TDestination>()
        where TSource : struct, Enum
        where TDestination : struct, Enum;
}