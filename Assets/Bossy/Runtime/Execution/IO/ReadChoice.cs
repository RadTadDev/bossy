namespace Bossy.Execution
{
    /// <summary>
    /// A choice offered by a <see cref="ReadRequest"/>.
    /// </summary>
    public sealed class ReadChoice
    {
        /// <summary>
        /// The text shown to the user for this choice.
        /// </summary>
        public string Display { get; }

        /// <summary>
        /// The value returned when this choice is picked.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Creates a read choice.
        /// </summary>
        /// <param name="display">The text shown to the user.</param>
        /// <param name="value">The value returned when picked.</param>
        public ReadChoice(string display, object value)
        {
            Display = display;
            Value = value;
        }
    }
}
