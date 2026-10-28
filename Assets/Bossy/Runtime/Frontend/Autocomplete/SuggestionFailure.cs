using Bossy.Schema;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// An argument's suggestion function or validator threw while the completions were made. The rest of the
    /// result is still valid; it just has no suggested values for this argument, or treats its value as valid.
    /// </summary>
    public class SuggestionFailure
    {
        /// <summary>
        /// The argument whose suggestion function or validator threw.
        /// </summary>
        public readonly ArgumentSchema Argument;

        /// <summary>
        /// The message of the exception.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// True if a validator threw, false if the suggestion function did.
        /// </summary>
        public readonly bool IsValidator;

        /// <summary>
        /// Makes a new suggestion failure.
        /// </summary>
        /// <param name="argument">The argument whose suggestion function or validator threw.</param>
        /// <param name="message">The message of the exception.</param>
        /// <param name="isValidator">True if a validator threw, false if the suggestion function did.</param>
        public SuggestionFailure(ArgumentSchema argument, string message, bool isValidator = false)
        {
            Argument = argument;
            Message = message;
            IsValidator = isValidator;
        }
    }
}
