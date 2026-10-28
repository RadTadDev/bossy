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
        /// Creates a token.
        /// </summary>
        /// <param name="text">The text of the token.</param>
        /// <param name="isOperator">True if this token is an unquoted operator.</param>
        /// <param name="isQuoted">True if any part of this token was quoted or escaped.</param>
        public Token(string text, bool isOperator = false, bool isQuoted = false)
        {
            Text = text;
            IsOperator = isOperator;
            IsQuoted = isQuoted;
        }
    }
}
