using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Utils;

namespace Bossy.Schema
{
    /// <summary>
    /// Builds a schema based on an input command type.
    /// </summary>
    internal static class CommandSchemaBuilder
    {
        private const BindingFlags AllBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        
        public static IReadOnlyList<CommandSchema> BuildCommandSchemas(CommandDependencyGraph graph)
        {
            var map = new Dictionary<Type, CommandSchema>();
            
            foreach (var (type, node) in graph)
            {
                if (!map.ContainsKey(type))
                {
                    map.Add(type, InitializeSchema(type));
                }
                
                // TODO: Use schema writer to set parents and children as they appear
            }
            
            // Check for naming collisions
            var fullyQualifiedNames = new HashSet<string>();
            foreach (var schema in map.Values.Where(s => s.ParentSchema == null))
            {
                DetectCollision(schema.Name, schema, fullyQualifiedNames);
            }

            return map.Values.ToList();
        }
        
        private static void DetectCollision(string fqn, CommandSchema schema, HashSet<string> visited)
        {
            if (!visited.Add(fqn))
            {
                var parts = fqn.Split('.');
                var path = string.Join("->", parts);
                    
                throw new BossyInitializationException($"The command name {path} appears more than once!");
            }

            foreach (var child in schema.ChildSchemas)
            {
                DetectCollision(fqn + $".{child.Name}", child, visited);
            }
        }
        
        private static CommandSchema InitializeSchema(Type type)
        {
            var argFields = type.GetFields(AllBindingFlags).Where(f => f.GetCustomAttribute<ArgumentAttribute>() != null);

            var args = new HashSet<ArgumentSchema>();
            
            foreach (var field in argFields)
            {
                args.Add(InitializeArgumentSchema(field));
            }
            
            var attribute = type.GetCustomAttribute<CommandAttribute>();

            var name = CommandMetaProcessor.CommandName(attribute.Name);
            var desc = CommandMetaProcessor.Description(attribute.Description);
            
            return new CommandSchema(name, desc, type, args);
        }

        private static ArgumentSchema InitializeArgumentSchema(FieldInfo field)
        {
            var attribute = field.GetCustomAttribute<ArgumentAttribute>();
            
            var name = CommandMetaProcessor.ArgumentName(attribute, field);
            var desc = CommandMetaProcessor.Description(attribute.Description);
            
            // TODO: Actually get validators when they exist
            return new ArgumentSchema(name, desc, field, attribute, null);
        }
    }
}