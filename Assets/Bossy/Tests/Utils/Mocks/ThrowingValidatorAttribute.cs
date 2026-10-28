using System;
using Bossy.Command;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A validator that always throws, for testing how validator exceptions are reported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ThrowingValidatorAttribute : ArgumentValidationAttribute
    {
        /// <summary>
        /// The message of the exception it throws.
        /// </summary>
        public const string Message = "Validator broken on purpose";

        public override ArgumentValidationResult Validate(object value)
        {
            throw new InvalidOperationException(Message);
        }

        public override string Describe()
        {
            return "broken check";
        }
    }
}
