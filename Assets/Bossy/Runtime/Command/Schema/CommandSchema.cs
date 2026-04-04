using System;
using System.Collections.Generic;

namespace Bossy.Command
{
    /// <summary>
    /// A readonly blueprint declaring the structure of a command.
    /// </summary>
    public class CommandSchema
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The description of this command.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// The type of this command.
        /// </summary>
        public readonly Type CommandType;

        /// <summary>
        /// All arguments for this command.
        /// </summary>
        public readonly HashSet<ArgumentSchema> Arguments;

        /// <summary>
        /// The parent schema of this command, or null is there is not one.
        /// </summary>
        public readonly CommandSchema ParentSchema;
        
        /// <summary>
        /// All child schemas of this command.
        /// </summary>
        public readonly HashSet<CommandSchema> ChildSchemas;
        
        /// <summary>
        /// Builds a new command schema.
        /// </summary>
        /// <param name="name">The name of this command.</param>
        /// <param name="description">The description of this command.</param>
        /// <param name="commandType">The type of this command.</param>
        /// <param name="arguments">All arguments for this command.</param>
        /// <param name="parentSchema">The parent schema of this command, or null is there is not one.</param>
        /// <param name="childSchemas">All child schemas of this command.</param>
        public CommandSchema(string name, string description, Type commandType, HashSet<ArgumentSchema> arguments, CommandSchema parentSchema, HashSet<CommandSchema> childSchemas)
        {
            Name = name;
            Description = description;
            CommandType = commandType;
            Arguments = arguments;
            ParentSchema = parentSchema;
            ChildSchemas = childSchemas;
        }
    }
}