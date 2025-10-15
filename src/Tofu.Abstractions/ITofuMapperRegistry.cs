namespace Tofu.Abstractions;

public interface ITofuMapperRegistry
{
    void Enum<TSource, TDestination>(Action<ITofuEnumMapBuilder<TSource, TDestination>>? configure = null);
}