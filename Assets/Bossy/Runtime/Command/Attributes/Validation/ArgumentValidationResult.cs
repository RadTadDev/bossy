namespace Bossy.Command
{
    /// <summary>
    /// Represents the result of an argument validation.
    /// </summary>
    public class ArgumentValidationResult
    {
        public readonly bool Success;
        public readonly string Message;

        private ArgumentValidationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>
        /// Creates a passed result.
        /// </summary>
        /// <returns>The result.</returns>
        public static ArgumentValidationResult Pass()
        {
            return new ArgumentValidationResult(true, string.Empty);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The result.</returns>
        public static ArgumentValidationResult Fail(string message)
        {
            return new ArgumentValidationResult(false, message);
        }
    }
}