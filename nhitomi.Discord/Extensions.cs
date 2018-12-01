// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public static class Array
    {
        public static bool IsNullOrEmpty(System.Array array) => array == null || array.Length == 0;
    }

    internal static class Extensions
    {
        public static string Format(this double[] elapsed)
        {
            var time = elapsed[0];
            if (time < 100)
                return $"{Math.Round(time, 3)}ms";
            if (time < 60000)
                return $"{Math.Round(time / 1000, 2)}s";

            return $"{Math.Round(time / 60000, 2)}m";
        }

        public static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T> source)
        {
            if (source.Any())
                return source;
            return null;
        }

        public static IEnumerable<T> Select<T>(this IEnumerable<T> source, Func<T, int, T> projection)
        {
            var index = 0;
            foreach (var item in source)
                yield return projection(item, index++);
        }

        public static async Task ModifyAsync(this IUserMessage message, string content = null, Embed embed = null)
        {
            if (string.IsNullOrWhiteSpace(content) && embed == null)
                await message.DeleteAsync();
            else
                await message.ModifyAsync(m =>
                {
                    m.Content = content;
                    m.Embed = embed;
                });
        }

        public static IDisposable Measure(out double[] elapsed) => new MeasureContext(elapsed = new double[1]);
        public class MeasureContext : IDisposable
        {
            public readonly double[] Store;
            public readonly Stopwatch Watch;

            public MeasureContext(double[] store)
            {
                Store = store;
                Watch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                Watch.Stop();
                Store[0] = Watch.Elapsed.TotalMilliseconds;
            }
        }

        public static IAsyncEnumerable<T> Interleave<T>(IEnumerable<IAsyncEnumerable<T>> source) => AsyncEnumerable.CreateEnumerable(() =>
        {
            var enumerators = source.Select(s => s.GetEnumerator()).ToList();
            var current = -1;

            return AsyncEnumerable.CreateEnumerator(
                moveNext: async token =>
                {
                    while (enumerators.Count > 0)
                    {
                        // Next enumerator
                        if (++current >= enumerators.Count)
                            current = 0;

                        if (await enumerators[current].MoveNext(token))
                            return true;

                        // Remove failing enumerators
                        enumerators.RemoveAt(current);
                    }

                    return false;
                },
                current: () => enumerators[current].Current,
                dispose: () =>
                {
                    foreach (var enumerator in enumerators)
                        enumerator.Dispose();
                }
            );
        });

        public static Task<T> AsCompletedTask<T>(this T obj) => Task.FromResult(obj);

        /// Computes and returns the Damerau-Levenshtein edit distance between two strings, 
        /// i.e. the number of insertion, deletion, sustitution, and transposition edits
        /// required to transform one string to the other. This value will be >= 0, where 0
        /// indicates identical strings. Comparisons are case sensitive, so for example, 
        /// "Fred" and "fred" will have a distance of 1. This algorithm is basically the
        /// Levenshtein algorithm with a modification that considers transposition of two
        /// adjacent characters as a single edit.
        /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
        /// </summary>
        /// <remarks>See http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
        /// Note that this is based on Sten Hjelmqvist's "Fast, memory efficient" algorithm, described
        /// at http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm.
        /// This version differs by including some optimizations, and extending it to the Damerau-
        /// Levenshtein algorithm.
        /// Note that this is the simpler and faster optimal string alignment (aka restricted edit) distance
        /// that difers slightly from the classic Damerau-Levenshtein algorithm by imposing the restriction
        /// that no substring is edited more than once. So for example, "CA" to "ABC" has an edit distance
        /// of 2 by a complete application of Damerau-Levenshtein, but a distance of 3 by this method that
        /// uses the optimal string alignment algorithm. See wikipedia article for more detail on this
        /// distinction.
        /// </remarks>
        /// <param name="s">String being compared for distance.</param>
        /// <param name="t">String being compared against other string.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>int edit distance, >= 0 representing the number of edits required
        /// to transform one string to the other, or -1 if the distance is greater than the specified maxDistance.</returns>
        public static int DamLev(this string s, string t, int maxDistance = int.MaxValue)
        {
            if (String.IsNullOrEmpty(s)) return ((t ?? "").Length <= maxDistance) ? (t ?? "").Length : -1;
            if (String.IsNullOrEmpty(t)) return (s.Length <= maxDistance) ? s.Length : -1;

            // if strings of different lengths, ensure shorter string is in s. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (s.Length > t.Length)
            {
                var temp = s; s = t; t = temp; // swap s and t
            }
            int sLen = s.Length; // this is also the minimun length of the two strings
            int tLen = t.Length;

            // suffix common to both strings can be ignored
            while ((sLen > 0) && (s[sLen - 1] == t[tLen - 1])) { sLen--; tLen--; }

            int start = 0;
            if ((s[0] == t[0]) || (sLen == 0))
            { // if there's a shared prefix, or all s matches t's suffix
              // prefix common to both strings can be ignored
                while ((start < sLen) && (s[start] == t[start])) start++;
                sLen -= start; // length of the part excluding common prefix and suffix
                tLen -= start;

                // if all of shorter string matches prefix and/or suffix of longer string, then
                // edit distance is just the delete of additional characters present in longer string
                if (sLen == 0) return (tLen <= maxDistance) ? tLen : -1;

                t = t.Substring(start, tLen); // faster than t[start+j] in inner loop below
            }
            int lenDiff = tLen - sLen;
            if ((maxDistance < 0) || (maxDistance > tLen))
            {
                maxDistance = tLen;
            }
            else if (lenDiff > maxDistance) return -1;

            var v0 = new int[tLen];
            var v2 = new int[tLen]; // stores one level further back (offset by +1 position)
            int j;
            for (j = 0; j < maxDistance; j++) v0[j] = j + 1;
            for (; j < tLen; j++) v0[j] = maxDistance + 1;

            int jStartOffset = maxDistance - (tLen - sLen);
            bool haveMax = maxDistance < tLen;
            int jStart = 0;
            int jEnd = maxDistance;
            char sChar = s[0];
            int current = 0;
            for (int i = 0; i < sLen; i++)
            {
                char prevsChar = sChar;
                sChar = s[start + i];
                char tChar = t[0];
                int left = i;
                current = left + 1;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < tLen) ? 1 : 0;
                for (j = jStart; j < jEnd; j++)
                {
                    int above = current;
                    int thisTransCost = nextTransCost;
                    nextTransCost = v2[j];
                    v2[j] = current = left; // cost of diagonal (substitution)
                    left = v0[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevtChar = tChar;
                    tChar = t[j];
                    if (sChar != tChar)
                    {
                        if (left < current) current = left;   // insertion
                        if (above < current) current = above; // deletion
                        current++;
                        if ((i != 0) && (j != 0)
                            && (sChar == prevtChar)
                            && (prevsChar == tChar))
                        {
                            thisTransCost++;
                            if (thisTransCost < current) current = thisTransCost; // transposition
                        }
                    }
                    v0[j] = current;
                }
                if (haveMax && (v0[i + lenDiff] > maxDistance)) return -1;
            }
            return (current <= maxDistance) ? current : -1;
        }

        // https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        public static string GetBytesReadable(this long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "eb";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "pd";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "tb";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "gb";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "mb";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "kb";
                readable = i;
            }
            else
            {
                return i.ToString("0 b"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}