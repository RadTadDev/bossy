using System;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The output of the autocomplete engine: an optional status message and a ranked list of completions.
    /// </summary>
    public class CompletionResult
    {
        /// <summary>
        /// A result with no message and no completions.
        /// </summary>
        public static readonly CompletionResult Empty = new(string.Empty, CompletionMessageKind.None, Array.Empty<Completion>());

        /// <summary>
        /// The status message. Empty if there is none.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// The kind of status message.
        /// </summary>
        public readonly CompletionMessageKind MessageKind;

        /// <summary>
        /// The completions, best first.
        /// </summary>
        public readonly IReadOnlyList<Completion> Completions;

        /// <summary>
        /// Details about the argument being typed when the message is a hint about it, otherwise null.
        /// </summary>
        public readonly ArgumentHint Argument;

        /// <summary>
        /// The failure of the suggestion function of the argument being completed, or of a validator that threw,
        /// otherwise null. It is kept out of <see cref="Message"/>, since the rest of the result is still valid.
        /// </summary>
        public readonly SuggestionFailure SuggestionFailure;

        /// <summary>
        /// True if there is nothing to show.
        /// </summary>
        public bool IsEmpty => MessageKind == CompletionMessageKind.None && Completions.Count == 0;

        private CompletionResult(string message, CompletionMessageKind messageKind, IReadOnlyList<Completion> completions,
            ArgumentHint argument = null, SuggestionFailure suggestionFailure = null)
        {
            Message = message;
            MessageKind = messageKind;
            Completions = completions;
            Argument = argument;
            SuggestionFailure = suggestionFailure;
        }

        /// <summary>
        /// Makes a result with completions and no message.
        /// </summary>
        /// <param name="completions">The completions, best first.</param>
        /// <returns>The result.</returns>
        public static CompletionResult List(IReadOnlyList<Completion> completions)
        {
            return new CompletionResult(string.Empty, CompletionMessageKind.None, completions);
        }

        /// <summary>
        /// Makes a result with a hint and optional completions.
        /// </summary>
        /// <param name="message">The hint.</param>
        /// <param name="completions">The completions, best first.</param>
        /// <param name="argument">Details about the argument the hint is about, if any.</param>
        /// <param name="suggestionFailure">The failure of the argument's suggestion function, if it threw.</param>
        /// <returns>The result.</returns>
        public static CompletionResult Hint(string message, IReadOnlyList<Completion> completions = null, ArgumentHint argument = null,
            SuggestionFailure suggestionFailure = null)
        {
            return new CompletionResult(message, CompletionMessageKind.Hint, completions ?? Array.Empty<Completion>(), argument, suggestionFailure);
        }

        /// <summary>
        /// Makes a result saying the line is ready to run, with optional completions for the parts that remain.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="completions">The completions, best first.</param>
        /// <param name="suggestionFailure">The failure of the next argument's suggestion function, if it threw.</param>
        /// <returns>The result.</returns>
        public static CompletionResult Ready(string message, IReadOnlyList<Completion> completions = null, SuggestionFailure suggestionFailure = null)
        {
            return new CompletionResult(message, CompletionMessageKind.Ready, completions ?? Array.Empty<Completion>(), null, suggestionFailure);
        }

        /// <summary>
        /// Makes a result with an error and no completions.
        /// </summary>
        /// <param name="message">The error.</param>
        /// <returns>The result.</returns>
        public static CompletionResult Error(string message)
        {
            return new CompletionResult(message, CompletionMessageKind.Error, Array.Empty<Completion>());
        }

        /// <summary>
        /// Makes a copy of this result whose completions for a slot carry the runs of their display text that match
        /// the text typed in it. Completions for other spans, such as those inside a quoted command, are kept as they are.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns>The new result.</returns>
        internal CompletionResult WithMatches(CompletionSlot slot)
        {
            if (string.IsNullOrEmpty(slot.Typed) || Completions.Count == 0) return this;

            var completions = Completions.Select(c => c.Start == slot.Start ? c.WithMatches(slot.Typed) : c).ToList();
            return WithCompletions(completions);
        }

        /// <summary>
        /// Makes a copy of this result with other completions.
        /// </summary>
        /// <param name="completions">The completions, best first.</param>
        /// <returns>The new result.</returns>
        internal CompletionResult WithCompletions(IReadOnlyList<Completion> completions)
        {
            return new CompletionResult(Message, MessageKind, completions, Argument, SuggestionFailure);
        }

        /// <summary>
        /// Makes a copy of this result that reports a failure, unless it already reports one.
        /// </summary>
        /// <param name="failure">The failure.</param>
        /// <returns>The new result.</returns>
        internal CompletionResult WithFailure(SuggestionFailure failure)
        {
            if (SuggestionFailure != null) return this;

            return new CompletionResult(Message, MessageKind, Completions, Argument, failure);
        }

        /// <summary>
        /// Makes a copy of this result with a completion added first. Errors stay without completions.
        /// </summary>
        /// <param name="completion">The completion to add.</param>
        /// <returns>The new result.</returns>
        internal CompletionResult WithFirst(Completion completion)
        {
            if (MessageKind == CompletionMessageKind.Error) return this;

            var completions = new List<Completion> { completion };
            completions.AddRange(Completions);
            return new CompletionResult(Message, MessageKind, completions, Argument, SuggestionFailure);
        }
    }
}
