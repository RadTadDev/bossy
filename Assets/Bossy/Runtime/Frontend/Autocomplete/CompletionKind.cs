namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// What a <see cref="Completion"/> completes.
    /// </summary>
    public enum CompletionKind
    {
        /// <summary>
        /// A root command name.
        /// </summary>
        Command,

        /// <summary>
        /// A subcommand name.
        /// </summary>
        Subcommand,

        /// <summary>
        /// An alias defined in the session.
        /// </summary>
        Alias,

        /// <summary>
        /// A switch name.
        /// </summary>
        Switch,

        /// <summary>
        /// A value for an argument.
        /// </summary>
        Value
    }
}
