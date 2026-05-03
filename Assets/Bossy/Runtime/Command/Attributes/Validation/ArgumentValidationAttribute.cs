using System;

namespace Bossy.Command
{
    /// <summary>
    /// Base class for all argument validation attributes. Used for discovering all validators on an argument.
    /// </summary>
    public abstract class ArgumentValidationAttribute : Attribute
    {
        /// <summary>
        /// Validates an argument.
        /// </summary>
        /// <param name="value">The argument value.</param>
        /// <returns>The result.</returns>
        public abstract ArgumentValidationResult Validate(object value);
    }
}