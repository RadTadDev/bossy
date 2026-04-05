using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;

namespace Bossy.Registry
{
    /// <summary>
    /// Defines the relationships between all commands. Represents a collection of graphs with each
    /// defining a single command relationship tree. Used in the first pass when building the registry.
    /// </summary>
    internal static class CommandDependencyGraph
    {
        /// <summary>
        /// Builds a map from type to dependency node.
        /// </summary>
        /// <param name="commandTypes">A list of all command types to graph.</param>
        /// <returns>The mapping.</returns>
        /// <exception cref="ArgumentException">Throws on command type.</exception>
        public static Dictionary<Type, CommandDependencyNode> BuildGraph(IReadOnlyList<Type> commandTypes)
        {
            var invalid = commandTypes.FirstOrDefault(t => !ReflectiveCommandDiscoverer.IsCommandType(t));

            if (invalid != null)
            {
                throw new ArgumentException($"Command {invalid.Name} is not a command type!");
            }
            
            var map = new Dictionary<Type, CommandDependencyNode>();

            foreach (var type in commandTypes)
            {
                var parentType = type.GetCustomAttribute<CommandAttribute>()!.ParentType;
                
                if (!map.ContainsKey(type))
                {
                    map[type] = new CommandDependencyNode();
                }

                map[type].Parent = parentType;

                if (parentType == null) continue;

                if (!commandTypes.Contains(parentType))
                {
                    throw new ArgumentException($"Command {type.FullName}'s parent type {parentType.FullName} is not a command type!");
                }
                
                if (!map.ContainsKey(parentType))
                {
                    map[parentType] = new CommandDependencyNode();
                }

                map[parentType].Children.Add(type);
            }
            
            return map;
        }
    }
}