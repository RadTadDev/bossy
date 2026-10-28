using Bossy.Execution;
using System.Collections.Generic;
using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// A single command and its link. The smallest parseable unit.
    /// </summary>
    internal class ParseNode
    {
        /// <summary>
        /// The link to the next node.
        /// </summary>
        public readonly CommandGraphLink Link;
        
        /// <summary>
        /// The tokens in this node.
        /// </summary>
        public readonly IReadOnlyList<Token> Tokens;

        /// <summary>
        /// The command that this node parsed to.
        /// </summary>
        public ICommand Command;
        
        /// <summary>
        /// The schema associated with this command.
        /// </summary>
        public CommandSchema Schema;

        /// <summary>
        /// The values bound to the command's arguments, keyed by argument.
        /// </summary>
        public Dictionary<ArgumentSchema, object> Values;

        /// <summary>
        /// Creates a new parse node.
        /// </summary>
        /// <param name="tokens">The tokens in this node.</param>
        /// <param name="link">The link to the next node.</param>
        public ParseNode(IReadOnlyList<Token> tokens, CommandGraphLink link)
        {
            Link = link;
            Tokens = tokens;
        }
    }
}