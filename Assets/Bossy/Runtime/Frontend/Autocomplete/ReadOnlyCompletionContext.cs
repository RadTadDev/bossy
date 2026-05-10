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
        public bool IsOnVariadic => _context.IsOnVariadic;

        /// <summary>
        /// True if the user has passed a "--" token, indicating all remaining switch-like tokens should not be considered switches.
        /// </summary>
        public bool ConsumedSwitchSurrender => _context.ConsumedSwitchSurrender;

        /// <summary>
        /// The remaining switches that have not yet been consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Switches => _context.Switches;
        
        /// <summary>
        /// The remaining positionals in order that they should be consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Positionals => _context.Positionals;
        
        /// <summary>
        /// The remaining optionals in order that they should be consumed.
        /// </summary>
        public IReadOnlyList<ArgumentSchema> Optionals => _context.Optionals;

        /// <summary>
        /// A list of the tokens consumed so far.
        /// </summary>
        public IReadOnlyList<string> TokensSoFar;
        
        /// <summary>
        /// The variadic arg. Null if this command does not have one.
        /// </summary>
        [CanBeNull] public ArgumentSchema Variadic => _context.Variadic;
        
        private AutocompleteEngine.CompletionContext _context;
        
        internal ReadOnlyCompletionContext(AutocompleteEngine.CompletionContext context, List<string> tokensSoFar)
        {
            _context = context;
            TokensSoFar = tokensSoFar;
        }
    }
}