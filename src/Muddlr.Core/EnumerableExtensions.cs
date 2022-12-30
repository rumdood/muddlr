namespace Muddlr;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> EmptyIfNull<TSource>(this IEnumerable<TSource>? source)
    {
        return source ?? Enumerable.Empty<TSource>();
    }
}
