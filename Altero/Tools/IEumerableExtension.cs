using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Tools
{
    public static class IEumerableExtension
    {
        public static void ForEach<T>(this IEnumerable<T> array, Action<T> action)
        {
            foreach (var elem in array)
                action(elem);
        }

        public static void ForEach<T>(this IEnumerable<T> array, Action<T,int> action)
        {
            int i = 0;
            foreach (var elem in array) {
                action(elem,i);
                i++;
            }
        }
    }
}
