namespace Tofu.Abstractions;

public interface ITofuMapperRegistry
{
    IEnumMappingConfiguration<TSource, TDestination> Enum<TSource, TDestination>()
        where TSource : struct, Enum
        where TDestination : struct, Enum;
}