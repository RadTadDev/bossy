using System.Collections.Generic;

namespace Bossy.Schema
{
    /// <summary>
    /// A container for the result of running a validation check against a schema.
    /// </summary>
    internal class ValidationResult
    {
        /// <summary>
        /// True if the schema was valid, otherwise false.
        /// </summary>
        public bool IsValid => Errors.Count == 0;
        
        /// <summary>
        /// True is the schema compilation contained warnings.
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;
        
        /// <summary>
        /// A list of all errors.
        /// </summary>
        public readonly IReadOnlyList<WarningContext> Warnings;
        
        /// <summary>
        /// A list of all errors.
        /// </summary>
        public readonly IReadOnlyList<ErrorContext> Errors;

        /// <summary>
        /// Builds a new schema validation result.
        /// </summary>
        /// <param name="warnings">A list of string warnings.</param>
        /// <param name="errors">A list of string errors.</param>
        public ValidationResult(IReadOnlyList<WarningContext> warnings, IReadOnlyList<ErrorContext> errors)
        {
            Errors = errors;
            Warnings = warnings;
        }
    }
}