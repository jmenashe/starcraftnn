using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN
{
    public static class Extensions
    {
        public static bool In<T>(this T item, params T[] list)
        {
            return item.In((IEnumerable<T>)list);
        }

        public static bool In<T>(this T item, IEnumerable<T> list)
        {
            foreach (var l in list)
            {
                if (item.Equals(l))
                    return true;
            }
            return false;
        }
    }
}
