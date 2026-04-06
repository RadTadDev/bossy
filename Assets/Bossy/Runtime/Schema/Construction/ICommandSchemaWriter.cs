using System.Collections.Generic;

namespace Bossy.Schema
{
    /// <summary>
    /// A writer interface to modify otherwise readonly state. 
    /// </summary>
    internal interface ICommandSchemaWriter
    {
        /// <summary>
        /// Sets the parent of this schema.
        /// </summary>
        /// <param name="parent">The parent schema.</param>
        public void SetParent(CommandSchema parent);
        
        /// <summary>
        /// Sets the children of this schema.
        /// </summary>
        /// <param name="children">The children schemas.</param>
        public void SetChildren(HashSet<CommandSchema> children);
    }
}