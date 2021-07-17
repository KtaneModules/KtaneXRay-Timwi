using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rnd = UnityEngine.Random;

namespace XRay
{
    static class Ut
    {
        public static T[] NewArray<T>(params T[] array) { return array; }

        /// <summary>
        ///     Instantiates a fully-initialized rectangular jagged array with the specified dimensions.</summary>
        /// <param name="size1">
        ///     Size of the first dimension.</param>
        /// <param name="size2">
        ///     Size of the second dimension.</param>
        /// <param name="initialiser">
        ///     Optional function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[][] NewArray<T>(int size1, int size2, Func<int, int, T> initialiser = null)
        {
            var result = new T[size1][];
            for (int i = 0; i < size1; i++)
            {
                var arr = new T[size2];
                if (initialiser != null)
                    for (int j = 0; j < size2; j++)
                        arr[j] = initialiser(i, j);
                result[i] = arr;
            }
            return result;
        }

        /// <summary>
        ///     Turns all elements in the enumerable to strings and joins them using the specified <paramref
        ///     name="separator"/> and the specified <paramref name="prefix"/> and <paramref name="suffix"/> for each string.</summary>
        /// <param name="values">
        ///     The sequence of elements to join into a string.</param>
        /// <param name="separator">
        ///     Optionally, a separator to insert between each element and the next.</param>
        /// <param name="prefix">
        ///     Optionally, a string to insert in front of each element.</param>
        /// <param name="suffix">
        ///     Optionally, a string to insert after each element.</param>
        /// <param name="lastSeparator">
        ///     Optionally, a separator to use between the second-to-last and the last element.</param>
        /// <example>
        ///     <code>
        ///         // Returns "[Paris], [London], [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]")
        ///         
        ///         // Returns "[Paris], [London] and [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]", " and ");</code></example>
        public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null, string lastSeparator = null)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (lastSeparator == null)
                lastSeparator = separator;

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return prefix + one + suffix;

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
                    if (prefix == null && suffix == null)
                        return one + lastSeparator + two;
                    return prefix + one + suffix + lastSeparator + prefix + two + suffix;
                }

                StringBuilder sb = new StringBuilder()
                    .Append(prefix).Append(one).Append(suffix).Append(separator)
                    .Append(prefix).Append(two).Append(suffix);
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    sb.Append(separator).Append(prefix).Append(prev).Append(suffix);
                    prev = enumerator.Current;
                }
                sb.Append(lastSeparator).Append(prefix).Append(prev).Append(suffix);
                return sb.ToString();
            }
        }

        public static T PickRandom<T>(this IEnumerable<T> src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            var arr = src.ToArray();
            if (arr.Length == 0)
                throw new InvalidOperationException("Cannot pick a random element from an empty set.");
            return arr[Rnd.Range(0, arr.Length)];
        }

        /// <summary>
        ///     Brings the elements of the given list into a random order.</summary>
        /// <typeparam name="T">
        ///     Type of elements in the list.</typeparam>
        /// <param name="list">
        ///     List to shuffle.</param>
        /// <returns>
        ///     The list operated on.</returns>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            for (int j = list.Count; j >= 1; j--)
            {
                int item = Rnd.Range(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
            return list;
        }
    }
}
