namespace Moneo.Core;

public static class EnumerableExtensions
{
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? collection)
    {
        return collection ?? Enumerable.Empty<T>();
    }

    public static bool HasMoreThan<T>(this IEnumerable<T> collection, int count)
    {
        return collection.Take(count + 1).Count() > count;
    }
}
