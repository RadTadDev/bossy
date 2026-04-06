using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Utils;

namespace Bossy.Schema
{
    /// <summary>
    /// Defines the relationships between all commands. Represents a collection of graphs with each
    /// defining a single command relationship tree. Used in the first pass when building the registry.
    /// </summary>
    internal static class CommandDependencyGraphBuilder
    {
        /// <summary>
        /// Builds a map from type to dependency node.
        /// </summary>
        /// <param name="commandTypes">A list of all command types to graph.</param>
        /// <returns>The command dependency graph.</returns>
        /// <exception cref="ArgumentException">Throws on circular dependency structure.</exception>
        public static CommandDependencyGraph BuildGraph(IReadOnlyList<Type> commandTypes)
        {
            var graph = new CommandDependencyGraph();

            foreach (var type in commandTypes)
            {
                var parentType = type.GetCustomAttribute<CommandAttribute>()!.ParentType;
                
                if (!graph.Contains(type))
                {
                    graph.AddNode(type);
                }

                graph.SetParent(type, parentType);

                if (parentType == null) continue;

                if (!commandTypes.Contains(parentType))
                {
                    throw new ArgumentException($"Command {type.FullName}'s parent type {parentType.FullName} is not a command type!");
                }
                
                if (!graph.Contains(parentType))
                {
                    graph.AddNode(parentType);
                }

                graph.AddChild(parentType, type);
            }
            
            // Ensure no cycles
            foreach (var type in commandTypes)
            {
                DetectCycle(type, graph, new HashSet<Type>());
            }
            
            return graph;
        }
        
        private static void DetectCycle(Type type, CommandDependencyGraph graph, HashSet<Type> visited)
        {
            if (!visited.Add(type))
            {
                throw new BossyInitializationException($"Circular command dependency detected involving {type.FullName}");
            }

            foreach (var child in graph.GetChildren(type))
            {
                DetectCycle(child, graph, visited);
            }
        }
    }
}