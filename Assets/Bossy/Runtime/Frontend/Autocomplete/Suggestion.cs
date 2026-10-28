namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// A value suggested by a suggestion function, with an optional description shown beside it. A string converts
    /// to a suggestion with no description, so suggestion functions may return strings instead.
    /// </summary>
    public readonly struct Suggestion
    {
        /// <summary>
        /// The value to insert. It is quoted automatically when it needs quotes.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// A description shown dimmed beside the value. Empty if there is none.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Makes a new suggestion.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <param name="description">A description shown beside the value.</param>
        public Suggestion(string value, string description = null)
        {
            Value = value;
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Makes a suggestion with no description.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The suggestion.</returns>
        public static implicit operator Suggestion(string value)
        {
            return new Suggestion(value);
        }
    }
}
