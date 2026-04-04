using System;

namespace Bossy.Command
{
    /// <summary>
    /// Base class for argument attributes used for discovering the type of an argument.
    /// </summary>
    public class ArgumentAttribute : Attribute
    {
        /// <summary>
        /// The name of this argument.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// The description of this argument.
        /// </summary>
        public readonly string Description;
        
        /// <summary>
        /// Declares a new argument.
        /// </summary>
        /// <param name="name">The name of this argument, pass in null to use the field name.</param>
        /// <param name="description">The description of this argument.</param>
        public ArgumentAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}