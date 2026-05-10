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

        /// <summary>
        /// A count indicating how many tokens were desired or consumed.
        /// </summary>
        public readonly int TokensDesired;
        
        private TypeAdapterResult(bool success, string errorMessage, int tokensDesired)
        {
            Success = success;
            ErrorMessage = errorMessage;
            TokensDesired = tokensDesired;
        }

        /// <summary>
        /// Create a passing type adapter result.
        /// </summary>
        /// <param name="tokensConsumed">The number of tokens consumed.</param>
        /// <returns>The result.</returns>
        public static TypeAdapterResult Pass(int tokensConsumed)
        {
            return new TypeAdapterResult(true, null, tokensConsumed);
        }

        /// <summary>
        /// Create a failing type adapter result.
        /// </summary>
        /// <param name="errorMessage">The specific error message.</param>
        /// <param name="tokensDesired">The number of tokens desired.</param>
        /// <returns>The result.</returns>
        public static TypeAdapterResult Fail(string errorMessage, int tokensDesired)
        {
            return new TypeAdapterResult(false, errorMessage, tokensDesired);
        }
    }
}