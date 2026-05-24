using Bossy.Utils;

namespace Bossy.Command
{
    /// <summary>
    /// Ensures a string is a valid file system path that exists on disk.
    /// </summary>
    public class IsExistingPathAttribute : ArgumentValidationAttribute
    {
        public override ArgumentValidationResult Validate(object value)
        {
            if (value is not string str)
            {
                return ArgumentValidationResult.Fail($"'{value}' ({value.GetType().GetFriendlyName()} must be a path that exists.");
            }

            if (string.IsNullOrWhiteSpace(str))
            {
                return ArgumentValidationResult.Fail("Path cannot be empty.");
            }

            if (str.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            {
                return ArgumentValidationResult.Fail($"'{str}' contains invalid path characters.");
            }

            if (!System.IO.Directory.Exists(str) && !System.IO.File.Exists(str))
            {
                return ArgumentValidationResult.Fail($"'{str}' does not exist.");
            }
            
            return ArgumentValidationResult.Pass();
        }
    }
}