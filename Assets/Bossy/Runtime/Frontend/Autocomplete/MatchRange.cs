namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// A run of characters in a completion's display text that matched the typed text, so views can highlight it.
    /// </summary>
    public readonly struct MatchRange
    {
        /// <summary>
        /// The index in the display text where the run starts.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The number of characters in the run.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// The index just past the end of the run.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// Makes a new match range.
        /// </summary>
        /// <param name="start">The index where the run starts.</param>
        /// <param name="length">The number of characters in the run.</param>
        public MatchRange(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}
