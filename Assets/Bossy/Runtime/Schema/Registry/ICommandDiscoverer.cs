using System;
using System.Collections.Generic;
using System.Reflection;
using Bossy.Command;

namespace Bossy.Schema.Registry
{
    /// <summary>
    /// Defines a strategy for discovering all command types.
    /// </summary>
    internal interface ICommandDiscoverer
    {
        /// <summary>
        /// Reflectively discovers all commands in the codebase.
        /// </summary>
        /// <returns>A list of all discovered command types.</returns>
        public IReadOnlyList<Type> GetAllCommandTypes();
        
        /// <summary>
        /// Tells if a type is a valid command type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if it is a command type, false otherwise.</returns>
        public static bool IsCommandType(Type type)
        {
            return type is { IsAbstract: false, IsInterface: false } &&
                   type.GetCustomAttribute<CommandAttribute>() is not null &&
                   typeof(ICommand).IsAssignableFrom(type);
        }
    }
}