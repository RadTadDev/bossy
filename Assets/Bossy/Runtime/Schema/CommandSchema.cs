using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;

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
        public IReadOnlyList<CommandSchema> ChildSchemas => _children;
        private readonly List<CommandSchema> _children = new(); 
            
        /// <summary>
        /// True if this command has no parent command.
        /// </summary>
        public bool IsRoot => ParentSchema == null;
        
        /// <summary>
        /// Builds a new command schema.
        /// </summary>
        /// <param name="name">The name of this command.</param>
        /// <param name="description">The description of this command.</param>
        /// <param name="commandType">The type of this command.</param>
        /// <param name="arguments">All arguments for this command.</param>
        public CommandSchema(string name, string description, Type commandType, HashSet<ArgumentSchema> arguments)
        {
            if (arguments == null) arguments = new HashSet<ArgumentSchema>();
            
            Name = name;
            Description = description;
            CommandType = commandType;
            Arguments = arguments;
        }
        
        /// <summary>
        /// Instantiates this schema into a command object.
        /// </summary>
        /// <returns>The command.</returns>
        public ICommand Instantiate() => (ICommand)Activator.CreateInstance(CommandType);

        /// <summary>
        /// Tries to look for a switch in the argument list.
        /// </summary>
        /// <param name="name">The name of the switch.</param>
        /// <param name="argument">The argument schema.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryFindSwitch(string name, out ArgumentSchema argument)
        {
            argument = Arguments
                .Where(a => a.ArgumentAttribute is SwitchAttribute)
                .FirstOrDefault(a => a.Name == name);
            
            return argument != null;
        }
        
        /// <summary>
        /// Tries to look for a switch in the argument list.
        /// </summary>
        /// <param name="shortName">The short name of the switch.</param>
        /// <param name="argument">The argument schema.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryFindSwitch(char shortName, out ArgumentSchema argument)
        {
            argument = Arguments
                .Where(a => a.ArgumentAttribute is SwitchAttribute)
                .FirstOrDefault(a => ((SwitchAttribute)a.ArgumentAttribute).ShortName == shortName);
            
            return argument != null;
        }
        
        /// <summary>
        /// Tries to look for a positional in the argument list.
        /// </summary>
        /// <param name="name">The name of the positional.</param>
        /// <param name="argument">The argument schema.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryFindPositional(string name, out ArgumentSchema argument)
        {
            argument = Arguments
                .Where(a => a.ArgumentAttribute is PositionalAttribute)
                .FirstOrDefault(a => a.Name == name);
            
            return argument != null;
        }
        
        /// <summary>
        /// Tries to look for an optional in the argument list.
        /// </summary>
        /// <param name="name">The name of the optional.</param>
        /// <param name="argument">The argument schema.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryFindOptional(string name, out ArgumentSchema argument)
        {
            argument = Arguments
                .Where(a => a.ArgumentAttribute is OptionalAttribute)
                .FirstOrDefault(a => a.Name == name);
            
            return argument != null;
        }
        
        /// <summary>
        /// Tries to look for a variadic in the argument list.
        /// </summary>
        /// <param name="name">The name of the variadic.</param>
        /// <param name="argument">The argument schema.</param>
        /// <returns>True if found, otherwise false.</returns>
        public bool TryFindVariadic(string name, out ArgumentSchema argument)
        {
            argument = Arguments
                .Where(a => a.ArgumentAttribute is VariadicAttribute)
                .FirstOrDefault(a => a.Name == name);
            
            return argument != null;
        }
        
        void ICommandSchemaWriter.SetParent(CommandSchema parent)
        {
            ParentSchema = parent;
        }

        void ICommandSchemaWriter.AddChild(CommandSchema child)
        {
            if (!_children.Contains(child))
            {
                _children.Add(child);
            }
        }
    }
}