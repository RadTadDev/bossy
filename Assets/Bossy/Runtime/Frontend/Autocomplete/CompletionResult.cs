using System;
using System.Collections.Generic;

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
        /// True if there is nothing to show.
        /// </summary>
        public bool IsEmpty => MessageKind == CompletionMessageKind.None && Completions.Count == 0;

        private CompletionResult(string message, CompletionMessageKind messageKind, IReadOnlyList<Completion> completions)
        {
            Message = message;
            MessageKind = messageKind;
            Completions = completions;
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
        /// <returns>The result.</returns>
        public static CompletionResult Hint(string message, IReadOnlyList<Completion> completions = null)
        {
            return new CompletionResult(message, CompletionMessageKind.Hint, completions ?? Array.Empty<Completion>());
        }

        /// <summary>
        /// Makes a result saying the line is ready to run, with optional completions for the parts that remain.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="completions">The completions, best first.</param>
        /// <returns>The result.</returns>
        public static CompletionResult Ready(string message, IReadOnlyList<Completion> completions = null)
        {
            return new CompletionResult(message, CompletionMessageKind.Ready, completions ?? Array.Empty<Completion>());
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
        /// Makes a copy of this result with a completion added first. Errors stay without completions.
        /// </summary>
        /// <param name="completion">The completion to add.</param>
        /// <returns>The new result.</returns>
        internal CompletionResult WithFirst(Completion completion)
        {
            if (MessageKind == CompletionMessageKind.Error) return this;

            var completions = new List<Completion> { completion };
            completions.AddRange(Completions);
            return new CompletionResult(Message, MessageKind, completions);
        }
    }
}
