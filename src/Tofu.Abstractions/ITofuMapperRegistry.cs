namespace Tofu.Abstractions;

public interface ITofuMapperRegistry
{
    ITofuEnumMappingConfiguration<TSource, TDestination> Enum<TSource, TDestination>()
        where TSource : struct, Enum
        where TDestination : struct, Enum;
}