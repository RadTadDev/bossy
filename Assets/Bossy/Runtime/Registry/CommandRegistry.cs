using System;
using System.Collections.Generic;
using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Registry
{
    /// <summary>
    /// A container holding a registry of all command schemas.
    /// </summary>
    internal class CommandRegistry
    {
        private Dictionary<string, CommandSchema> _registry = new();
        
        /// <summary>
        /// Creates a command registry.
        /// </summary>
        /// <param name="schemas">A list of all command schemas the registry should provide.</param>
        public CommandRegistry(IReadOnlyList<CommandSchema> schemas)
        {
            // TODO: Only store root commands in dictionary, children can be searched for
            // by indexing globally unique root command, then searching children.
        }

        /// <summary>
        /// Gets a command based on its name.
        /// </summary>
        /// <param name="root">The root command name.</param>
        /// <param name="command">The command if found.</param>
        /// <returns>True if a matching command was found, otherwise false.</returns>
        public bool ResolveCommand(string root, out ICommand command)
        {
            return ResolveCommand(root, Array.Empty<string>(), out command);
        }

        /// <summary>
        /// Gets a command based on its name.
        /// </summary>
        /// <param name="root">The root command name.</param>
        /// <param name="subcommands">Zero or more sub command names.</param>
        /// <param name="command">The command if found.</param>
        /// <returns>True if a matching command was found, otherwise false.</returns>
        public bool ResolveCommand(string root, IEnumerable<string> subcommands, out ICommand command)
        {
            // TODO: Use Activator to return an instance of the command type in a matching schema.

            command = null;
            return false;
        }
    }
}