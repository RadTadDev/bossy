using Bossy.Shell;
using System.Collections.Generic;
using Bossy.Command;

namespace Bossy.FrontEnd.Parsing
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
        public readonly IEnumerable<string> Tokens;

        /// <summary>
        /// The command that this node parsed to.
        /// </summary>
        public ICommand Command;
        
        /// <summary>
        /// Creates a new parse node.
        /// </summary>
        /// <param name="tokens">The tokens in this node.</param>
        /// <param name="link">The link to the next node.</param>
        public ParseNode(IEnumerable<string> tokens, CommandGraphLink link)
        {
            Link = link;
            Tokens = tokens;
        }
    }
}