namespace Dango.Abstractions;

/// <summary>
/// Defines a registrar that configures enum mappings for Dango source generation.
/// Implement this interface in your project to register enum mappings that will be generated at compile time.
/// </summary>
public interface IDangoMapperRegistrar
{
    /// <summary>
    /// Registers enum mappings with the provided registry.
    /// This method is called by the Dango source generator to discover and configure all enum mappings.
    /// </summary>
    /// <param name="registry">The registry to configure enum mappings with.</param>
    void Register(IDangoMapperRegistry registry);
}