using System;
using System.Collections.Generic;

namespace Bossy.Registry
{
    /// <summary>
    /// Defines the relationship between command types.
    /// </summary>
    public class CommandDependencyNode
    {
        /// <summary>
        /// The parent of this node.
        /// </summary>
        public Type Parent;
        
        /// <summary>
        /// Set of all children of this node.
        /// </summary>
        public readonly HashSet<Type> Children = new();
    }
}