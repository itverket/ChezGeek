using System;
using System.Collections.Generic;
using System.Linq;

namespace ChezGeek.Team2
{
    public static class Extensions
    {
        public static TValue GetRandomItem<TValue>(this ICollection<TValue> values, Random random)
        {
            return values.ElementAt(random.Next(0, values.Count));
        }
    }
}