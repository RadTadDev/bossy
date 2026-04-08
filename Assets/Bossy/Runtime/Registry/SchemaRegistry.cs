using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Registry
{
    /// <summary>
    /// A container holding a registry of all command schemas.
    /// </summary>
    internal class SchemaRegistry
    {
        private readonly Dictionary<string, CommandSchema> _registry = new();
        
        /// <summary>
        /// Creates a schema registry.
        /// </summary>
        /// <param name="schemas">A list of all command schemas the registry should provide.</param>
        public SchemaRegistry(IReadOnlyList<CommandSchema> schemas)
        {
            foreach (var schema in schemas)
            {
                if (schema.IsRoot)
                {
                    _registry.Add(schema.Name, schema);
                }
            }
        }

        /// <summary>
        /// Gets a schema based on its name.
        /// </summary>
        /// <param name="root">The root command name.</param>
        /// <param name="schema">The schema if found.</param>
        /// <returns>True if a matching schema was found, otherwise false.</returns>
        public bool TryResolveSchema(string root, out CommandSchema schema)
        {
            return TryResolveSchema(root, Array.Empty<string>(), out schema);
        }

        /// <summary>
        /// Gets a command schema based on its name.
        /// </summary>
        /// <param name="root">The root command name.</param>
        /// <param name="subcommands">Zero or more subcommand names.</param>
        /// <param name="schema">The schema if found.</param>
        /// <returns>True if a matching schema was found, otherwise false.</returns>
        public bool TryResolveSchema(string root, IEnumerable<string> subcommands, out CommandSchema schema)
        {
            schema = null;

            if (!_registry.TryGetValue(root, out schema))
            {
                return false;
            }

            foreach (var subcommand in subcommands)
            {
                var child = schema.ChildSchemas.FirstOrDefault(c => c.Name == subcommand);
                
                if (child == null) return false;

                schema = child;
            }
            
            return true;
        }
    }
}