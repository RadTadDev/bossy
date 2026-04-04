using System;

namespace Bossy.Command
{
    /// <summary>
    /// Declares that a class is a command and allows autoregistration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Type ParentType;
        
        /// <summary>
        /// Defines the metadata of a command.
        /// </summary>
        /// <param name="name">The name of the command - used on the command line to invoke it.</param>
        /// <param name="description">A short description of what the command does.</param>
        /// <param name="parentType">Optional - The type of this command's parent.</param>
        public CommandAttribute(string name, string description, Type parentType = null)
        {
            Name = name;
            Description = description;
            ParentType = parentType;
        }
    }
}