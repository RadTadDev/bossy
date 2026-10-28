namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The kind of status message in a <see cref="CompletionResult"/>.
    /// </summary>
    public enum CompletionMessageKind
    {
        /// <summary>
        /// There is no message.
        /// </summary>
        None,

        /// <summary>
        /// Information about what is being typed, such as the argument at the cursor.
        /// </summary>
        Hint,

        /// <summary>
        /// The command line is complete and can be run.
        /// </summary>
        Ready,

        /// <summary>
        /// The command line will not parse. A result with an error has no completions.
        /// </summary>
        Error
    }
}
