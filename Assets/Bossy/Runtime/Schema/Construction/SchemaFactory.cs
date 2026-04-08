using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Utils;

namespace Bossy.Schema
{
    /// <summary>
    /// Builds command schemas.
    /// </summary>
    internal static class SchemaFactory
    {
        private const BindingFlags AllBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        
        public static IReadOnlyList<CommandSchema> BuildCommandSchemas(CommandDependencyGraph graph)
        {
            var map = new Dictionary<Type, CommandSchema>();
            
            // Pass 1: Initialize all schema without relationships
            foreach (var (type, _) in graph)
            {
                map.Add(type, InitializeSchema(type));
            }

            // Pass 2: Wire parents and children
            foreach (var (type, node) in graph)
            {
                ICommandSchemaWriter writer = map[type];

                if (node.Parent != null)
                {
                    writer.SetParent(map[node.Parent]);
                }

                foreach (var child in node.Children)
                {
                    writer.AddChild(map[child]); 
                }
            }
            
            // Pass 3: Check for naming collisions
            var fullyQualifiedNames = new HashSet<string>();
            
            foreach (var schema in map.Values.Where(s => s.IsRoot))
            {
                DetectCollision(schema.Name, schema, fullyQualifiedNames);
            }

            return map.Values.ToList();
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
        
        private static void DetectCollision(string fqn, CommandSchema schema, HashSet<string> visited)
        {
            if (!visited.Add(fqn))
            {
                var parts = fqn.Split('.');
                var path = string.Join("->", parts);
                    
                throw new BossyInitializationException($"The command name \"{path}\" appears more than once!");
            }

            foreach (var child in schema.ChildSchemas)
            {
                DetectCollision(fqn + $".{child.Name}", child, visited);
            }
        }
    }
}