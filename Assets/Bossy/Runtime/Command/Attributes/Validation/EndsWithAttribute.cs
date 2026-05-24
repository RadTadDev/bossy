using Bossy.Utils;

namespace Bossy.Command
{
    /// <summary>
    /// Ensures a string starts with a particular string.
    /// </summary>
    public class EndsWithAttribute : ArgumentValidationAttribute
    {
        /// <summary>
        /// The suffix that this argument must end with.
        /// </summary>
        private readonly string _suffix;
        
        /// <summary>
        /// Enforces that this command ends with a given prefix if it is a string.
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        public EndsWithAttribute(string suffix)
        {
            _suffix = suffix;
        }
        
        public override ArgumentValidationResult Validate(object value)
        {
            if (value is not string str)
            {
                return ArgumentValidationResult.Fail($"'{value}' ({value.GetType().GetFriendlyName()}) must be a string and end with '{_suffix}'.");
            }

            if (!str.EndsWith(_suffix))
            {
                return ArgumentValidationResult.Fail($"'{str}' must end with '{_suffix}'.");
            }

            return ArgumentValidationResult.Pass();
        }
    }
}