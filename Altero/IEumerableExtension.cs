using System;
using System.Collections.Generic;
using System.Text;

namespace Altero
{
    public static class IEumerableExtension
    {
        public static void ForEach<T>(this IEnumerable<T> array, Action<T> action)
        {
            foreach (var elem in array)
                action(elem);
        }
    }
}
