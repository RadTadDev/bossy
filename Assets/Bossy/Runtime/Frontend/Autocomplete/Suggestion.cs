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
        public readonly string DisplayTest;
        
        /// <summary>
        /// Makes a new suggestion.
        /// </summary>
        /// <param name="fullText">The full text to suggest.</param>
        /// <param name="displayTest">The text to display.</param>
        public Suggestion(string fullText, string displayTest)
        {
            FullText = fullText;
            DisplayTest = displayTest;
        }
    }
}