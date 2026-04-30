namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// Holds information about the result of a type conversion attempt.
    /// </summary>
    public struct TypeAdapterResult
    {
        /// <summary>
        /// Whether the result was successful.
        /// </summary>
        public readonly bool Success;
        
        /// <summary>
        /// An error message about what went wrong. This is NULL if the result is successful.
        /// </summary>
        public readonly string ErrorMessage;
        
        private TypeAdapterResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Create a passing type adapter result.
        /// </summary>
        /// <returns>The result.</returns>
        public static TypeAdapterResult Pass()
        {
            return new TypeAdapterResult(true, null);
        }

        /// <summary>
        /// Create a failing type adapter result.
        /// </summary>
        /// <param name="errorMessage">The specific error message.</param>
        /// <returns>The result.</returns>
        public static TypeAdapterResult Fail(string errorMessage)
        {
            return new TypeAdapterResult(false, errorMessage);
        }
    }
}