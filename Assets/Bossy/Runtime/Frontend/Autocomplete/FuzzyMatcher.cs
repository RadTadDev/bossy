using System;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Ranks candidates against typed text in tiers: exact, prefix, start of a segment, substring, then fuzzy.
    /// Candidates in the same tier keep the order they were given in, and fuzzy matches are ordered by score.
    /// </summary>
    internal static class FuzzyMatcher
    {
        // TODO: Make this a Bossy setting once settings are implemented.
        private const float FuzzyThreshold = 0.4f;

        private enum Tier
        {
            Exact,
            Prefix,
            SegmentStart,
            Substring,
            Fuzzy,
            None
        }

        /// <summary>
        /// Ranks candidates against a query. Candidates that do not match at all are left out. An empty query keeps
        /// every candidate in its original order.
        /// </summary>
        /// <param name="query">The typed text.</param>
        /// <param name="candidates">The candidates, in the order to keep for ties.</param>
        /// <param name="key">Gets the text to match from a candidate.</param>
        /// <typeparam name="T">The candidate type.</typeparam>
        /// <returns>The matching candidates, best first.</returns>
        public static List<T> Rank<T>(string query, IEnumerable<T> candidates, Func<T, string> key)
        {
            if (string.IsNullOrEmpty(query)) return candidates.ToList();

            var lowerQuery = query.ToLowerInvariant();
            var queryTrigrams = GetTrigrams(lowerQuery);
            var levenshteinRow = new List<int>();
            var ranked = new List<(Tier Tier, float Score, int Index, T Candidate)>();

            var index = 0;
            foreach (var candidate in candidates)
            {
                var text = key(candidate);
                var tier = GetTier(lowerQuery, text, out var score, queryTrigrams, levenshteinRow);

                if (tier != Tier.None)
                {
                    ranked.Add((tier, score, index, candidate));
                }

                index++;
            }

            return ranked
                .OrderBy(r => r.Tier)
                .ThenByDescending(r => r.Score)
                .ThenBy(r => r.Index)
                .Select(r => r.Candidate)
                .ToList();
        }

        /// <summary>
        /// Finds the runs of a text that match a query, by the same tiers as <see cref="Rank{T}"/>. A fuzzy match
        /// has no clear run of matching characters, so it gets no ranges.
        /// </summary>
        /// <param name="query">The typed text.</param>
        /// <param name="text">The text to find matches in.</param>
        /// <returns>The matching runs, in order. Empty if the query is empty or only matches fuzzily.</returns>
        public static List<MatchRange> FindMatches(string query, string text)
        {
            var ranges = new List<MatchRange>();
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(text)) return ranges;

            var lowerQuery = query.ToLowerInvariant();
            var lowerText = text.ToLowerInvariant();

            if (lowerText.StartsWith(lowerQuery, StringComparison.Ordinal))
            {
                ranges.Add(new MatchRange(0, query.Length));
                return ranges;
            }

            if (MatchesSegmentStart(lowerQuery, text, ranges)) return ranges;

            var index = lowerText.IndexOf(lowerQuery, StringComparison.Ordinal);
            if (index >= 0)
            {
                ranges.Add(new MatchRange(index, query.Length));
            }

            return ranges;
        }

        private static Tier GetTier(string lowerQuery, string text, out float score, HashSet<string> queryTrigrams, List<int> levenshteinRow)
        {
            score = 0;
            var lowerText = text.ToLowerInvariant();

            if (lowerText == lowerQuery) return Tier.Exact;
            if (lowerText.StartsWith(lowerQuery, StringComparison.Ordinal)) return Tier.Prefix;
            if (MatchesSegmentStart(lowerQuery, text, null)) return Tier.SegmentStart;
            if (lowerText.Contains(lowerQuery)) return Tier.Substring;

            score = Math.Max(TrigramScore(queryTrigrams, lowerText), LevenshteinScore(lowerQuery, lowerText, levenshteinRow));
            return score >= FuzzyThreshold ? Tier.Fuzzy : Tier.None;
        }

        /*
         * A segment starts after a separator or at a lower-to-upper case change, so "unity-log" and "SetActive"
         * both have two segments. The query matches if a later segment starts with it ("log" -> "unity-log") or
         * if it spells out the first letters of the segments ("ul" -> "unity-log", "sa" -> "SetActive").
         * The matched characters are added to the ranges, if given.
         */
        private static bool MatchesSegmentStart(string lowerQuery, string text, List<MatchRange> ranges)
        {
            var initials = new List<int>();

            for (var i = 0; i < text.Length; i++)
            {
                if (!IsSegmentStart(text, i)) continue;

                initials.Add(i);

                if (i > 0 && string.Compare(text, i, lowerQuery, 0, lowerQuery.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ranges?.Add(new MatchRange(i, lowerQuery.Length));
                    return true;
                }
            }

            if (lowerQuery.Length < 2 || initials.Count < lowerQuery.Length) return false;

            for (var k = 0; k < lowerQuery.Length; k++)
            {
                if (char.ToLowerInvariant(text[initials[k]]) != lowerQuery[k]) return false;
            }

            ranges?.AddRange(initials.Take(lowerQuery.Length).Select(i => new MatchRange(i, 1)));
            return true;
        }

        private static bool IsSegmentStart(string text, int i)
        {
            if (!char.IsLetterOrDigit(text[i])) return false;
            if (i == 0) return true;

            var previous = text[i - 1];
            return !char.IsLetterOrDigit(previous) || (char.IsUpper(text[i]) && char.IsLower(previous));
        }

        /*
         * A normalized trigram score, computed as 2 * |intersection| / (|query| + |candidate|), where |x| is the
         * number of distinct trigrams in x.
         */
        private static float TrigramScore(HashSet<string> queryTrigrams, string lowerText)
        {
            var textTrigrams = GetTrigrams(lowerText);
            var total = queryTrigrams.Count + textTrigrams.Count;

            return total == 0 ? 0 : 2.0f * queryTrigrams.Count(textTrigrams.Contains) / total;
        }

        /*
         * A normalized Levenshtein score: one minus the number of single character edits needed to turn one
         * string into the other, divided by the longer length. The row buffer is reused between candidates.
         */
        private static float LevenshteinScore(string a, string b, List<int> row)
        {
            row.Clear();
            for (var j = 0; j <= b.Length; j++) row.Add(j);

            for (var i = 1; i <= a.Length; i++)
            {
                var diagonal = row[0];
                row[0] = i;

                for (var j = 1; j <= b.Length; j++)
                {
                    var above = row[j];
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    row[j] = Math.Min(Math.Min(above + 1, row[j - 1] + 1), diagonal + cost);
                    diagonal = above;
                }
            }

            var longest = Math.Max(a.Length, b.Length);
            return longest == 0 ? 1 : 1.0f - (float)row[b.Length] / longest;
        }

        private static HashSet<string> GetTrigrams(string word)
        {
            var result = new HashSet<string>();

            for (var i = 0; i + 3 <= word.Length; i++)
            {
                result.Add(word.Substring(i, 3));
            }

            return result;
        }
    }
}
