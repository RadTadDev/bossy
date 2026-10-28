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

        private static ArgumentHint HintWith(params ArgumentValidationAttribute[] validators)
        {
            var field = typeof(HintTarget).GetField(nameof(HintTarget.Value));
            var argument = new ArgumentSchema("value", "A value.", field, new PositionalAttribute(0, "A value."),
                new List<ArgumentValidationAttribute>(validators));

            return new ArgumentHint(argument, 1, 1, null);
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
