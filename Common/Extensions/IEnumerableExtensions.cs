using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

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

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
          this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending = true) 
            => ascending ? source.OrderBy(keySelector): source.OrderByDescending(keySelector);

        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, bool ascending = true)
            => OrderBy(source, k => k, ascending);

        public static string StrJoin<T>(this IEnumerable<T> source, string str)
            => string.Join(str, source);


    }
}
