using System.Globalization;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// A single token from an input line.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// The text of the token, with quotes and escapes removed.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// True if this token is an unquoted operator.
        /// </summary>
        public readonly bool IsOperator;

        /// <summary>
        /// True if any part of this token was quoted or escaped. Such a token is always literal text, so it is
        /// never an operator or a switch.
        /// </summary>
        public readonly bool IsQuoted;

        /// <summary>
        /// The index in the input line where this token starts, including any opening quote.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The index in the input line just past the end of this token, including any closing quote.
        /// </summary>
        public readonly int End;

        /// <summary>
        /// True if this token names a switch: unquoted text starting with a dash that is not a negative number.
        /// </summary>
        public bool IsSwitch => !IsOperator && !IsQuoted && Text.StartsWith("-") && !IsNegativeNumber(Text);

        /// <summary>
        /// The expansion of the alias this token came from, when the alias was typed as an argument. Null for typed
        /// tokens and for tokens of an alias typed as a command name. Every token of one expansion has the span of
        /// the alias.
        /// </summary>
        internal readonly string AliasExpansion;

        /// <summary>
        /// Creates a token.
        /// </summary>
        /// <param name="text">The text of the token.</param>
        /// <param name="isOperator">True if this token is an unquoted operator.</param>
        /// <param name="isQuoted">True if any part of this token was quoted or escaped.</param>
        /// <param name="start">The index in the input line where this token starts.</param>
        /// <param name="end">The index in the input line just past the end of this token.</param>
        public Token(string text, bool isOperator = false, bool isQuoted = false, int start = 0, int end = 0)
            : this(text, isOperator, isQuoted, start, end, null)
        {
        }

        internal Token(string text, bool isOperator, bool isQuoted, int start, int end, string aliasExpansion)
        {
            Text = text;
            IsOperator = isOperator;
            IsQuoted = isQuoted;
            Start = start;
            End = end;
            AliasExpansion = aliasExpansion;
        }

        private static bool IsNegativeNumber(string text)
        {
            // Short switch names are always letters, so a dash followed by a number is never a switch
            return text.Length > 1
                   && (char.IsDigit(text[1]) || text[1] == '.')
                   && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
