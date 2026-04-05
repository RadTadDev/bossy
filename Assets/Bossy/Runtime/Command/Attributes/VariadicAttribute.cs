using System;

namespace Bossy.Command
{
    /// <summary>
    /// Declares variadic arguments for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VariadicAttribute : ArgumentAttribute
    {
        /// <summary>
        /// Declares variadic arguments.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="name">The override name - defaults to field name if null.</param>
        /// <remarks>This variadic attribute may only be applied to array types.</remarks>
        public VariadicAttribute(string description, string name = null) : base(name, description) { }
    }
}