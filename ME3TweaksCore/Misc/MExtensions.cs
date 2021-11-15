using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.Misc
{
    /// <summary>
    /// ME3TweaksCore extensions
    /// </summary>
    public static class MExtensions
    {
        private static Random rng = new Random();

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[rng.Next(list.Count)];
        }

        public static T RandomElement<T>(this T[] array)
        {
            return array[rng.Next(array.Length)];
        }

        public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> values, IEqualityComparer<T> comparer = null)
        {
            return values.All(value => source.Contains(value, comparer));
        }
        public static bool ContainsNone<T>(this IEnumerable<T> source, IEnumerable<T> values, IEqualityComparer<T> comparer = null)
        {
            return !values.Any(value => source.Contains(value, comparer));
        }

        public static bool ContainsAny(this string input, IEnumerable<string> containsKeywords, StringComparison comparisonType)
        {
            return containsKeywords.Any(keyword => input.IndexOf(keyword, comparisonType) >= 0);
        }

        /// <summary>
        /// Converts an MEGame enum to an integer representation used by MassEffectModder. MEM is split into programs for OT and LE which both use the same indexing.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static int ToMEMGameNum(this MEGame game)
        {
            if (game == MEGame.ME1) return 1;
            if (game == MEGame.ME2) return 2;
            if (game == MEGame.ME3) return 3;
            if (game == MEGame.LE1) return 1;
            if (game == MEGame.LE2) return 2;
            if (game == MEGame.LE3) return 3;
            return 0;
        }

        private static readonly byte[] utf8Preamble = Encoding.UTF8.GetPreamble();

        public static string DownloadStringAwareOfEncoding(this WebClient webClient, string uri)
        {
            var rawData = webClient.DownloadData(uri);
            if (rawData.StartsWith(utf8Preamble))
            {
                return Encoding.UTF8.GetString(rawData, utf8Preamble.Length, rawData.Length - utf8Preamble.Length);
            }
            var encoding = WebUtils.GetEncodingFrom(webClient.ResponseHeaders, new UTF8Encoding(false));
            return encoding.GetString(rawData).Normalize();
        }

        private static bool StartsWith(this byte[] thisArray, byte[] otherArray)
        {
            // Handle invalid/unexpected input
            // (nulls, thisArray.Length < otherArray.Length, etc.)

            for (int i = 0; i < otherArray.Length; ++i)
            {
                if (thisArray[i] != otherArray[i])
                {
                    return false;
                }
            }

            return true;
        }

    }
}
