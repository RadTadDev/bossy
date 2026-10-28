using Bossy.Frontend.Parsing;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The place in the input line that completions replace: either the token at the cursor, or an empty span at
    /// the cursor when the next token is being predicted.
    /// </summary>
    internal readonly struct CompletionSlot
    {
        /// <summary>
        /// The index where the replaced span starts.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The index just past the end of the replaced span.
        /// </summary>
        public readonly int End;

        /// <summary>
        /// The unquoted text typed in the token before the cursor. Completions are filtered on this.
        /// </summary>
        public readonly string Typed;

        /// <summary>
        /// True if the token at the cursor was quoted, so inserted values keep the quotes.
        /// </summary>
        public readonly bool IsQuoted;

        /// <summary>
        /// True if there is no token at the cursor and the next token is being predicted.
        /// </summary>
        public readonly bool IsEmpty;

        /// <summary>
        /// Text added after each inserted completion, so it does not run into the token after the cursor.
        /// </summary>
        public readonly string Suffix;

        /// <summary>
        /// True if the token at the cursor names a switch.
        /// </summary>
        public bool IsSwitch => !IsEmpty && new Token(Typed, isQuoted: IsQuoted).IsSwitch;

        private CompletionSlot(int start, int end, string typed, bool isQuoted, bool isEmpty, string suffix)
        {
            Start = start;
            End = end;
            Typed = typed;
            IsQuoted = isQuoted;
            IsEmpty = isEmpty;
            Suffix = suffix;
        }

        /// <summary>
        /// Makes a slot for the token at the cursor.
        /// </summary>
        /// <param name="token">The token at the cursor.</param>
        /// <param name="typed">The unquoted text of the token before the cursor.</param>
        /// <returns>The slot.</returns>
        public static CompletionSlot ForToken(Token token, string typed)
        {
            return new CompletionSlot(token.Start, token.End, typed, token.IsQuoted, false, string.Empty);
        }

        /// <summary>
        /// Makes an empty slot at the cursor, for predicting the next token.
        /// </summary>
        /// <param name="cursor">The cursor position.</param>
        /// <param name="beforeToken">True if a token starts right at the cursor.</param>
        /// <returns>The slot.</returns>
        public static CompletionSlot At(int cursor, bool beforeToken)
        {
            return new CompletionSlot(cursor, cursor, string.Empty, false, true, beforeToken ? " " : string.Empty);
        }
    }
}
