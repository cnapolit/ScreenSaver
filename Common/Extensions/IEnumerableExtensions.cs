using System;
using System.Collections.Generic;

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
    }
}
