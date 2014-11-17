using System;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    public static class EnumerableExtensions
    {
        // Helper function to do some initialization before
        public static IEnumerable<T> Init<T>(this IEnumerable<T> enumerable, Action init)
        {
            init();
            return enumerable;
        }

        // Helper function for dynamically splitting strings.
        // From http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990.
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> input, Func<T, bool> controller, int max = 0)
        {
            var nextPiece = 0;
            var count = 0;

            for (var c = 0; c < input.Count() && (count < max || max == 0); c++)
            {
                if (controller(input.ElementAt(c)))
                {
                    yield return input.Where((value, row) => row >= nextPiece && row < c);
                    nextPiece = c + 1;
                }
            }

            yield return input.Where((value, row) => row >= nextPiece);
        }

        // Helper function to convert a set of IEnumerable<char> to strings.
        public static IEnumerable<string> ToStringSet(this IEnumerable<IEnumerable<char>> enumerable)
        {
            return enumerable.Select(v => new string(v.ToArray()));
        }

        // Helper function to convert a set of IEnumerable<char> to strings.
        public static string[] ToStringArray(this IEnumerable<IEnumerable<char>> enumerable)
        {
            return enumerable.ToStringSet().ToArray();
        }
    }
}

