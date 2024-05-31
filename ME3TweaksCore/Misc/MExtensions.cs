using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Compression;
using LegendaryExplorerCore.Helpers;
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

        public static bool StartsWith(this byte[] thisArray, byte[] otherArray)
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

        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly (@"c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(this string path, string baseDirPath)
        {
            string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .WithEnding(@"\\"));

            string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .WithEnding(@"\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding(@"llo") returns "hello", which is the result of "hel" + "lo".</example>
        private static string WithEnding(this string str, string ending)
        {
            if (str == null)
                return ending;

            string result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int i = 0; i <= ending.Length; i++)
            {
                string tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending))
                    return tmp;
            }

            return result;
        }

        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="value">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        private static string Right(this string value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException(@"value");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(@"length", length, @"Length is less than zero");
            }

            return (length < value.Length) ? value.Substring(value.Length - length) : value;
        }

        /// <summary>
        /// Reads an LZMA compressed string
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadCompressedUnrealString(this Stream stream)
        {
            var decompressedSize = stream.ReadUInt32();
            var compressedSize = stream.ReadUInt32();
            var binary = stream.ReadToBuffer(compressedSize);
            var decompressed = LZMA.Decompress(binary, decompressedSize);
            var ms = new MemoryStream(decompressed);
            return ms.ReadUnrealString();
        }

        /// <summary>
        /// Writes an LZMA compressed string
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="str"></param>
        public static void WriteCompressedUnrealString(this Stream stream, string str)
        {
            using MemoryStream ms = new MemoryStream();
            ms.WriteUnrealString(str, MEGame.LE3); // LE3 forces unicode.
            stream.WriteUInt32((uint)ms.Length); // Decompressed size
            var compressed = LZMA.Compress(ms.ToArray());
            stream.WriteUInt32((uint)compressed.Length);
            stream.Write(compressed);
        }
    }
}
