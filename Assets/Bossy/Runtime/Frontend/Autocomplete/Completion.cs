using System;
using System.Collections.Generic;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// A completion from the autocomplete engine. Applying it replaces a span of the input line with new text.
    /// </summary>
    public class Completion
    {
        /// <summary>
        /// The index in the input line where the replaced span starts.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The index in the input line just past the end of the replaced span.
        /// </summary>
        public readonly int End;

        /// <summary>
        /// The text that replaces the span, quoted when needed.
        /// </summary>
        public readonly string InsertText;

        /// <summary>
        /// The text to show for this completion.
        /// </summary>
        public readonly string DisplayText;

        /// <summary>
        /// A description shown beside the completion. Empty if there is none.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// What this completion completes.
        /// </summary>
        public readonly CompletionKind Kind;

        /// <summary>
        /// The runs of <see cref="DisplayText"/> that matched the typed text, in order, so views can highlight them.
        /// Empty if nothing was typed or the match was fuzzy.
        /// </summary>
        public readonly IReadOnlyList<MatchRange> MatchRanges;

        /// <summary>
        /// The cursor position just past the inserted text, once the completion is applied.
        /// </summary>
        public int CursorAfterApply => Start + InsertText.Length;

        /// <summary>
        /// Makes a new completion.
        /// </summary>
        /// <param name="start">The index where the replaced span starts.</param>
        /// <param name="end">The index just past the end of the replaced span.</param>
        /// <param name="insertText">The text that replaces the span.</param>
        /// <param name="displayText">The text to show.</param>
        /// <param name="description">A description shown beside the completion.</param>
        /// <param name="kind">What this completion completes.</param>
        public Completion(int start, int end, string insertText, string displayText, string description, CompletionKind kind)
            : this(start, end, insertText, displayText, description, kind, Array.Empty<MatchRange>())
        {
        }

        private Completion(int start, int end, string insertText, string displayText, string description, CompletionKind kind,
            IReadOnlyList<MatchRange> matchRanges)
        {
            Start = start;
            End = end;
            InsertText = insertText;
            DisplayText = displayText;
            Description = description ?? string.Empty;
            Kind = kind;
            MatchRanges = matchRanges;
        }

        /// <summary>
        /// Applies this completion to the line it was made for.
        /// </summary>
        /// <param name="line">The input line the completion was made for.</param>
        /// <returns>The line with the span replaced.</returns>
        public string ApplyTo(string line)
        {
            return line[..Start] + InsertText + line[End..];
        }

        /// <summary>
        /// Makes a copy of this completion with the runs of its display text that match a query.
        /// </summary>
        /// <param name="query">The typed text.</param>
        /// <returns>The new completion.</returns>
        internal Completion WithMatches(string query)
        {
            return new Completion(Start, End, InsertText, DisplayText, Description, Kind, FuzzyMatcher.FindMatches(query, DisplayText));
        }
    }
}
