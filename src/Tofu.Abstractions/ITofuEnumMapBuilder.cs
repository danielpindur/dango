namespace Tofu.Abstractions;

public interface ITofuEnumMapBuilder<TSource, TDestination>
{
    ITofuEnumMapBuilder<TSource, TDestination> Map(TSource source, TDestination destination);
}