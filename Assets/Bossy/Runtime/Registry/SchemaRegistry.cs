using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Schema;
using Bossy.Utils;

namespace Bossy.Registry
{
    /// <summary>
    /// A container holding a registry of all command schemas.
    /// </summary>
    internal class SchemaRegistry
    {
        private readonly Dictionary<string, CommandSchema> _registry = new();
        
        private readonly Dictionary<CommandSchema, ValidationResult> _invalidSchemas = new();
        
        /// <summary>
        /// Creates a schema registry.
        /// </summary>
        /// <param name="schemas">A list of all command schemas the registry should provide.</param>
        public SchemaRegistry(IReadOnlyList<CommandSchema> schemas)
        {
            foreach (var schema in schemas)
            {
                var validator = new SchemaValidator();

                var result = validator.Validate(schema);
                    
                if (!result.IsValid)
                {
                    _invalidSchemas.Add(schema, result);
                }

                // Allow invalid commands into the registry so we can query them for error displays
                
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
        public SchemaQueryStatus TryResolveSchema(string root, out CommandSchema schema)
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
        public SchemaQueryStatus TryResolveSchema(string root, IEnumerable<string> subcommands, out CommandSchema schema)
        {
            schema = null;

            // Validate root first
            if (!_registry.TryGetValue(root, out schema))
            {
                return SchemaQueryStatus.NotFound;
            }

            if (_invalidSchemas.ContainsKey(schema))
            {
                return SchemaQueryStatus.Invalid;
            }
            
            // Then validate any children
            foreach (var subcommand in subcommands)
            {
                var child = schema.ChildSchemas.FirstOrDefault(c => c.Name == subcommand);
                
                if (child == null) return SchemaQueryStatus.NotFound;
                
                if (_invalidSchemas.ContainsKey(child)) return SchemaQueryStatus.Invalid;
                
                schema = child;
            }
            
            return SchemaQueryStatus.Found;
        }

        /// <summary>
        /// Gets a list of all schemas. 
        /// </summary>
        /// <param name="commandPath">If specified, only the children of the command path are returned.</param>
        /// <returns>The list of schemas.</returns>
        public IEnumerable<CommandSchema> GetValidSchemas(IEnumerable<string> commandPath = null)
        {
            if (commandPath == null) return _registry.Values;

            var path = commandPath.ToList();
            
            if (!path.Any()) return _registry.Values;
            
            var root = path[0];
            path.RemoveAt(0);

            if (TryResolveSchema(root, path, out var rootSchema) is not SchemaQueryStatus.Found)
            {
                return Enumerable.Empty<CommandSchema>();
            }

            var result = new List<CommandSchema> { rootSchema };

            foreach (var schema in rootSchema.ChildSchemas)
            {
                AddChildren(schema, result);
            }
            
            // Filter out invalid schemas
            var invalidSchemas = GetInvalidSchemas();

            foreach (var invalid in invalidSchemas)
            {
                result.Remove(invalid);
            }
            
            return result;

            void AddChildren(CommandSchema schema, List<CommandSchema> allSchemas)
            {
                allSchemas.Add(schema);
            
                foreach (var child in schema.ChildSchemas)
                {
                    AddChildren(child, allSchemas);
                }
            }
        }

        /// <summary>
        /// Gets all invalid schemas.
        /// </summary>
        /// <returns>All invalid schemas.</returns>
        public IEnumerable<CommandSchema> GetInvalidSchemas()
        {
            return _invalidSchemas.Keys;
        }

        /// <summary>
        /// Gets the validation result for a schema.
        /// </summary>
        /// <param name="schema">The schema to get for.</param>
        /// <returns>The validation result.</returns>
        public ValidationResult GetValidationResult(CommandSchema schema)
        {
            if (schema == null) return new ValidationResult(null, new[] { new NullSchemaError() });
            return _invalidSchemas.TryGetValue(schema, out var result) ? result : new ValidationResult(null, null);
        }
    }
}