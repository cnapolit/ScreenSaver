using System;
using System.Collections.Generic;
using System.Linq;

namespace ScreenSaver.Common.Extensions
{
    internal static class IEnumerableExtensions
    {
        public static bool ForAny<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var result = false;
            items.ForEach(i => result |= predicate(i));
            return result;
        }
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending) 
            => ascending ? source.OrderBy(keySelector): source.OrderByDescending(keySelector);
    }
}
