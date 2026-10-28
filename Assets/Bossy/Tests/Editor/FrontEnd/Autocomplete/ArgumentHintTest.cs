using System.Collections.Generic;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using Bossy.Schema;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Autocomplete
{
    /// <summary>
    /// Tests the <see cref="ArgumentHint"/> constraints, which come from <see cref="ArgumentValidationAttribute.Describe"/>.
    /// </summary>
    internal class ArgumentHintTest
    {
        [Test]
        public void Constraints_BuiltInValidators_DescribeThemselves()
        {
            var hint = HintWith(
                new Bossy.Command.RangeAttribute(0.5f, float.MaxValue),
                new EndsWithAttribute(".cs"),
                new StartsWithAttribute("Assets"),
                new IsPathAttribute(),
                new IsExistingPathAttribute());

            Assert.That(hint.Constraints, Is.EqualTo(new[]
            {
                "range [0.5, ∞]", "ends with .cs", "starts with Assets", "path", "existing path"
            }));
        }

        [Test]
        public void Constraints_ValidatorWithoutDescription_IsLeftOut()
        {
            var hint = HintWith(new SilentValidator(), new EndsWithAttribute(".cs"));

            Assert.That(hint.Constraints, Is.EqualTo(new[] { "ends with .cs" }));
        }

        [Test]
        public void Constraints_NoValidators_IsEmpty()
        {
            Assert.That(HintWith().Constraints, Is.Empty);
        }

        [Test]
        public void FailedConstraint_IndexesDescribedConstraints()
        {
            var endsWith = new EndsWithAttribute(".cs");
            var argument = ArgumentWith(new SilentValidator(), new StartsWithAttribute("Assets"), endsWith);

            var hint = new ArgumentHint(argument, 1, 1, null, endsWith, "'x' must end with '.cs'.");

            Assert.That(hint.FailedConstraint, Is.EqualTo(1));
            Assert.That(hint.ValidationError, Is.EqualTo("'x' must end with '.cs'."));
        }

        [Test]
        public void FailedConstraint_UndescribedValidator_IsMinusOne()
        {
            var silent = new SilentValidator();

            var hint = new ArgumentHint(ArgumentWith(silent, new EndsWithAttribute(".cs")), 1, 1, null, silent, "Nope.");

            Assert.That(hint.FailedConstraint, Is.EqualTo(-1));
            Assert.That(hint.ValidationError, Is.EqualTo("Nope."));
        }

        [Test]
        public void FailedConstraint_NoFailure_IsMinusOne()
        {
            var hint = HintWith(new EndsWithAttribute(".cs"));

            Assert.That(hint.FailedConstraint, Is.EqualTo(-1));
            Assert.That(hint.ValidationError, Is.Null);
        }

        private static ArgumentHint HintWith(params ArgumentValidationAttribute[] validators)
        {
            return new ArgumentHint(ArgumentWith(validators), 1, 1, null);
        }

        private static ArgumentSchema ArgumentWith(params ArgumentValidationAttribute[] validators)
        {
            var field = typeof(HintTarget).GetField(nameof(HintTarget.Value));
            return new ArgumentSchema("value", "A value.", field, new PositionalAttribute(0, "A value."),
                new List<ArgumentValidationAttribute>(validators));
        }

        private class HintTarget
        {
            public string Value;
        }

        private class SilentValidator : ArgumentValidationAttribute
        {
            public override ArgumentValidationResult Validate(object value)
            {
                return ArgumentValidationResult.Pass();
            }
        }
    }
}
