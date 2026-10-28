using Bossy.Schema;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// An argument's suggestion function threw while the completions were made. The rest of the result is still
    /// valid; it just has no suggested values for this argument.
    /// </summary>
    public class SuggestionFailure
    {
        /// <summary>
        /// The argument whose suggestion function threw.
        /// </summary>
        public readonly ArgumentSchema Argument;

        /// <summary>
        /// The message of the exception.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Makes a new suggestion failure.
        /// </summary>
        /// <param name="argument">The argument whose suggestion function threw.</param>
        /// <param name="message">The message of the exception.</param>
        public SuggestionFailure(ArgumentSchema argument, string message)
        {
            Argument = argument;
            Message = message;
        }
    }
}
