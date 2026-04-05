using System;

namespace Bossy.Command
{
    /// <summary>
    /// Declares a positional argument for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PositionalAttribute : ArgumentAttribute
    {
        /// <summary>
        /// The 0-indexed order in which this positional appears relative to other positional args.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Declares a positional argument.
        /// </summary>
        /// <param name="index">The 0-indexed order in which this positional appears relative to other positional args.</param>
        /// <param name="description">The description.</param>
        /// <param name="name">The override name - defaults to field name if null.</param>
        public PositionalAttribute(int index, string description, string name = null) : base(name, description)
        {
            Index = index;
        }
    }
}