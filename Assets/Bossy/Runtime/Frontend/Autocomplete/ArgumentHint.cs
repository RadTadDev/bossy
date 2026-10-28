using System.Collections.Generic;
using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Details about the argument being typed, so a view can lay out, style, and shorten the hint itself instead of
    /// showing one long line. <see cref="CompletionResult.Message"/> holds a short plain rendering of the same hint.
    /// </summary>
    public class ArgumentHint
    {
        /// <summary>
        /// The argument being typed. Its name, type, description, and kind (from its argument attribute) are here.
        /// </summary>
        public readonly ArgumentSchema Argument;

        /// <summary>
        /// Which token of the value is being typed, from 1. Only meaningful when <see cref="TokensDesired"/> is more than 1.
        /// </summary>
        public readonly int TokenNumber;

        /// <summary>
        /// How many tokens the value takes, such as 3 for a Vector3.
        /// </summary>
        public readonly int TokensDesired;

        /// <summary>
        /// The typed value if it has all its tokens but does not parse, otherwise null.
        /// </summary>
        public readonly string InvalidValue;

        /// <summary>
        /// Short descriptions of the argument's validators, such as "ends with .cs". Validators that do not
        /// describe themselves are left out.
        /// </summary>
        public readonly IReadOnlyList<string> Constraints;

        /// <summary>
        /// The message of the validator the value at the cursor fails, or null if it passes them all or does not
        /// parse yet. The value is still being typed, so this is not an error yet.
        /// </summary>
        public readonly string ValidationError;

        /// <summary>
        /// The index in <see cref="Constraints"/> of the constraint the value fails, or -1 if it fails none, or
        /// fails a validator that does not describe itself.
        /// </summary>
        public readonly int FailedConstraint;

        /// <summary>
        /// True if the value takes more than one token, so <see cref="TokenNumber"/> shows progress.
        /// </summary>
        public bool HasProgress => TokensDesired > 1;

        /// <summary>
        /// Makes a new argument hint.
        /// </summary>
        /// <param name="argument">The argument being typed.</param>
        /// <param name="tokenNumber">Which token of the value is being typed, from 1.</param>
        /// <param name="tokensDesired">How many tokens the value takes.</param>
        /// <param name="invalidValue">The typed value if it is complete but does not parse, otherwise null.</param>
        /// <param name="failedValidator">The validator the value fails, if any.</param>
        /// <param name="validationError">The message of the failed validator, if any.</param>
        public ArgumentHint(ArgumentSchema argument, int tokenNumber, int tokensDesired, string invalidValue,
            ArgumentValidationAttribute failedValidator = null, string validationError = null)
        {
            Argument = argument;
            TokenNumber = tokenNumber;
            TokensDesired = tokensDesired;
            InvalidValue = invalidValue;
            ValidationError = validationError;
            FailedConstraint = -1;

            var constraints = new List<string>();
            foreach (var validator in argument.Validators)
            {
                var description = validator.Describe();
                if (string.IsNullOrWhiteSpace(description)) continue;

                if (validator == failedValidator)
                {
                    FailedConstraint = constraints.Count;
                }

                constraints.Add(description);
            }

            Constraints = constraints;
        }
    }
}
