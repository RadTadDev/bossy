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
        public readonly string Prefix;
        
        /// <summary>
        /// Enforces that this command starts with a given prefix if it is a string.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        public StartsWithAttribute(string prefix)
        {
            Prefix = prefix;
        }
        
        public override ArgumentValidationResult Validate(object value)
        {
            if (value is string str)
            {
                if (!str.StartsWith(Prefix))
                {
                    return ArgumentValidationResult.Fail($"'{str}' must start with '{Prefix}'.");
                }
            }

            return ArgumentValidationResult.Pass();
        }
    }
    
    /// <summary>
    /// Ensures a string starts with a particular string.
    /// </summary>
    public class EndsWithAttribute : ArgumentValidationAttribute
    {
        
        /// <summary>
        /// The suffix that this argument must end with.
        /// </summary>
        public readonly string Suffix;
        
        /// <summary>
        /// Enforces that this command ends with a given prefix if it is a string.
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        public EndsWithAttribute(string suffix)
        {
            Suffix = suffix;
        }
        
        public override ArgumentValidationResult Validate(object value)
        {
            if (value is string str)
            {
                if (!str.EndsWith(Suffix))
                {
                    return ArgumentValidationResult.Fail($"'{str}' must end with '{Suffix}'.");
                }
            }

            return ArgumentValidationResult.Pass();
        }
    }
}