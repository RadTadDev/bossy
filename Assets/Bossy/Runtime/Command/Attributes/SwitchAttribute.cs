using System;

namespace Bossy.Command
{
    /// <summary>
    /// Declares a switch argument for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SwitchAttribute : ArgumentAttribute
    {
        /// <summary>
        /// The single letter identifier for this switch.
        /// </summary>
        public readonly char ShortName;
        
        /// <summary>
        /// Declares a switch argument.
        /// </summary>
        /// <param name="shortName">The single letter name.</param>
        /// <param name="description">The description.</param>
        /// <param name="fullName">The full name - defaults to field name if null.</param>
        public SwitchAttribute(char shortName, string description, string fullName = null) : base(fullName, description)
        {
            ShortName = shortName;
        }
    }
}