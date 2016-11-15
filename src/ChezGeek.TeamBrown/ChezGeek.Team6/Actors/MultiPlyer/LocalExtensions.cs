using System;
using System.Collections.Generic;
using System.Linq;

namespace ChezGeek.TeamBrown.Actors.MultiPlyer
{
    public static class LocalExtensions
    {
        public static TValue GetRandomItem<TValue>(this ICollection<TValue> values, Random random)
        {
            return values.ElementAt(random.Next(0, values.Count));
        }

        public static IEnumerable<T>[] ToBulkArray<T>(this IEnumerable<T> enumerable, int bulkSize)
        {
            var array = enumerable.ToArray();
            var parts = (array.Length - 1)/bulkSize + 1;
            return Split(array, parts).ToArray();
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
        {
            if (parts == 0)
                parts++;
            var i = 0;
            return list
                .GroupBy(name => i++%parts)
                .Select(part => part.AsEnumerable());
        }
    }
}