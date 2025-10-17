namespace Tofu.Abstractions;

public interface IEnumMappingConfiguration<TSource, TDestination>
    where TSource : struct, Enum
    where TDestination : struct, Enum
{
    IEnumMappingConfiguration<TSource, TDestination> MapByValue();
    IEnumMappingConfiguration<TSource, TDestination> MapByName();
}