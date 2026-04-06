using System;
using System.Collections;
using System.Collections.Generic;

namespace Bossy.Schema
{
    /// <summary>
    /// A graph of command type dependencies listing each types parent and children.
    /// </summary>
    internal class CommandDependencyGraph : IEnumerable<KeyValuePair<Type, CommandDependencyNode>>
    {
        private readonly Dictionary<Type, CommandDependencyNode> _graph = new();
        
        /// <summary>
        /// Tells if a command type is already registered in the graph.
        /// </summary>
        /// <param name="type">The command type to query.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool Contains(Type type) => _graph.ContainsKey(type);
        
        /// <summary>
        /// Adds a new node to the graph.
        /// </summary>
        /// <param name="type">The command type to add.</param>
        public void AddNode(Type type) => _graph[type] = new CommandDependencyNode();

        /// <summary>
        /// Sets the parent of a command type.
        /// </summary>
        /// <param name="type">The command type whose parent to set.</param>
        /// <param name="parent">The parent command type.</param>
        public void SetParent(Type type, Type parent) => _graph[type].Parent = parent;
        
        /// <summary>
        /// Adds a child for a command type.
        /// </summary>
        /// <param name="type">The command type to add to.</param>
        /// <param name="child">The child command type.</param>
        public void AddChild(Type type, Type child) => _graph[type].Children.Add(child);

        /// <summary>
        /// Gets an enumerator for the graph to iterate each node.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<Type, CommandDependencyNode>> GetEnumerator() => _graph.GetEnumerator();

        /// <summary>
        /// Returns a type's children.
        /// </summary>
        /// <param name="type">The type to query for.</param>
        /// <returns>A collection of children.</returns>
        public IEnumerable<Type> GetChildren(Type type) => _graph[type].Children;
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}