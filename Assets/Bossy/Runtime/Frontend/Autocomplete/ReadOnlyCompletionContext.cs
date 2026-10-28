using System.Collections.Generic;
using Bossy.Schema;
using JetBrains.Annotations;

namespace Bossy.Frontend.Autocomplete
{
    public class ReadOnlyCompletionContext
    {
        /// <summary>
        /// True if the current input is ready for or already handling variadic tokens.
        /// </summary>
        public bool IsOnVariadic => _cursor.IsOnVariadic;

        /// <summary>
        /// True if the user has passed a "--" token, indicating all remaining switch-like tokens should not be considered switches.
        /// </summary>
        public bool ConsumedSwitchSurrender => _cursor.Surrendered;

        /// <summary>
        /// The remaining switches that have not yet been consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Switches => _cursor.Switches;

        /// <summary>
        /// The remaining positionals in order that they should be consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Positionals => _cursor.Positionals;

        /// <summary>
        /// The remaining optionals in order that they should be consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Optionals => _cursor.Optionals;

        /// <summary>
        /// A list of the tokens consumed so far.
        /// </summary>
        public IReadOnlyList<string> TokensSoFar;

        /// <summary>
        /// The variadic arg. Null if this command does not have one.
        /// </summary>
        [CanBeNull] public ArgumentSchema Variadic => _cursor.Variadic;

        private readonly ArgumentCursor _cursor;

        internal ReadOnlyCompletionContext(ArgumentCursor cursor, List<string> tokensSoFar)
        {
            _cursor = cursor;
            TokensSoFar = tokensSoFar;
        }
    }
}
