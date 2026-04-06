using System;
using System.Collections.Generic;

namespace Bossy.Schema
{
    /// <summary>
    /// A readonly blueprint declaring the structure of a command.
    /// </summary>
    internal class CommandSchema : ICommandSchemaWriter
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
        public CommandSchema ParentSchema { get; private set; }
        
        /// <summary>
        /// All child schemas of this command.
        /// </summary>
        public HashSet<CommandSchema> ChildSchemas { get; private set; }
        
        /// <summary>
        /// Builds a new command schema.
        /// </summary>
        /// <param name="name">The name of this command.</param>
        /// <param name="description">The description of this command.</param>
        /// <param name="commandType">The type of this command.</param>
        /// <param name="arguments">All arguments for this command.</param>
        public CommandSchema(string name, string description, Type commandType, HashSet<ArgumentSchema> arguments)
        {
            Name = name;
            Description = description;
            CommandType = commandType;
            Arguments = arguments;
        }

        void ICommandSchemaWriter.SetParent(CommandSchema parent)
        {
            ParentSchema = parent;
        }

        void ICommandSchemaWriter.SetChildren(HashSet<CommandSchema> children)
        {
            ChildSchemas = children;
        }
    }
}