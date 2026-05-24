using Bossy.Utils;

namespace Bossy.Command
{
    /// <summary>
    /// Ensures a string starts with a particular string.
    /// </summary>
    public class StartsWithAttribute : ArgumentValidationAttribute
    {
        /// <summary>
        /// The prefix that this argument must start with.
        /// </summary>
        private readonly string _prefix;
        
        /// <summary>
        /// Enforces that this command starts with a given prefix if it is a string.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        public StartsWithAttribute(string prefix)
        {
            _prefix = prefix;
        }
        
        public override ArgumentValidationResult Validate(object value)
        {
            if (value is not string str)
            {
                return ArgumentValidationResult.Fail($"'{value}' ({value.GetType().GetFriendlyName()}) must be a string and start with '{_prefix}'.");
            }
            
            if (!str.StartsWith(_prefix))
            {
                return ArgumentValidationResult.Fail($"'{str}' must start with '{_prefix}'.");
            }

            return ArgumentValidationResult.Pass();
        }
    }
}