namespace Tofu.Abstractions;

public interface ITofuEnumMappingConfiguration<TSource, TDestination>
    where TSource : struct, Enum
    where TDestination : struct, Enum
{
    ITofuEnumMappingConfiguration<TSource, TDestination> MapByValue();
    
    ITofuEnumMappingConfiguration<TSource, TDestination> MapByName();
    
    ITofuEnumMappingConfiguration<TSource, TDestination> WithDefault(TDestination defaultValue);
    
    ITofuEnumMappingConfiguration<TSource, TDestination> WithOverrides(
        IReadOnlyDictionary<TSource, TDestination> overrides);
}