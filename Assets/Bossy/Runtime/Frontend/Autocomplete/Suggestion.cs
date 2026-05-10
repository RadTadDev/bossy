namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// A suggestion from the autocomplete engine.
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// The full line being suggested.
        /// </summary>
        public readonly string FullText;

        /// <summary>
        /// The text to display as a suggestion.
        /// </summary>
        public readonly string DisplayText;

        /// <summary>
        /// True if this suggestion is a hint rather than something to be applied.
        /// </summary>
        public readonly bool IsHint;

        /// <summary>
        /// True if this suggestion is an error rather than something to be applied.
        /// </summary>
        public readonly bool IsError;

        /// <summary>
        /// Makes a new suggestion.
        /// </summary>
        /// <param name="fullText">The full text to suggest.</param>
        /// <param name="displayText">The text to display.</param>
        /// <param name="isHint">True if this suggestion is a hint and thus not able to be applied.</param>
        /// <param name="isError">True if this suggestion is an error and thus not able to be applied.</param>
        public Suggestion(string fullText, string displayText, bool isHint = false, bool isError = false)
        {
            FullText = fullText;
            DisplayText = displayText;
            IsHint = isHint;
            IsError = isError;
        }
    }
}