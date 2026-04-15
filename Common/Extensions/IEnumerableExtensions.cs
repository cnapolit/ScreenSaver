using Playnite;

namespace ScreenSaver.Common.Extensions;

internal static class IEnumerableExtensions
{
    public static bool ForAny<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        var result = false;
        items.ForEach(i => result |= predicate(i));
        return result;
    }

    public static IOrderedEnumerable<TSource> Order<TSource, TKey>(
      this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending = true) 
        => ascending ? source.OrderBy(keySelector): source.OrderByDescending(keySelector);

    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, bool ascending = true)
        => Order(source, k => k, ascending);

    public static string StrJoin<T>(this IEnumerable<T> source, string str)
        => string.Join(str, source);

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        => source.Where(x => x != null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.



}
