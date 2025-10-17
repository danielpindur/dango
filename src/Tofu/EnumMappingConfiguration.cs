using Tofu.Abstractions;

namespace Tofu;

internal sealed class EnumMappingConfiguration<TSource, TDestination>
    : IEnumMappingConfiguration<TSource, TDestination>
    where TSource : struct, Enum
    where TDestination : struct, Enum
{
    public MappingStrategy Strategy { get; private set; } = MappingStrategy.ByName;

    public IEnumMappingConfiguration<TSource, TDestination> MapByValue()
    {
        Strategy = MappingStrategy.ByValue;
        return this;
    }

    public IEnumMappingConfiguration<TSource, TDestination> MapByName()
    {
        Strategy = MappingStrategy.ByName;
        return this;
    }
}