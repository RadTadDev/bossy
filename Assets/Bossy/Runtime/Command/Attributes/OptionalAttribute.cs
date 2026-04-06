using System;

namespace Bossy.Command
{
    /// <summary>
    /// Declares an optional argument for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OptionalAttribute : ArgumentAttribute
    {
        /// <summary>
        /// The 0-indexed order in which this positional appears relative to other positional args.
        /// </summary>
        public readonly int Index;
        
        /// <summary>
        /// Declares an optional argument.
        /// </summary>
        /// <param name="index">The 0-indexed order in which this optional arg appears relative to other optional args.</param>
        /// <param name="description">The description.</param>
        /// <param name="overrideName">The override name - defaults to field name if null.</param>
        /// <remarks>All optional arguments should be input after all positional arguments on the command line.</remarks>
        public OptionalAttribute(int index, string description, string overrideName = null) : base(overrideName, description)
        {
            Index = index;
        }
    }
}