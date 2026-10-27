using Bossy.Frontend;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Bossy.Tests.FrontEnd.Console
{
    /// <summary>
    /// Tests the rule for when the toggle key types instead of toggling.
    /// </summary>
    public class ToggleKeyTest
    {
        [Test] public void ShouldType_NothingFocused_False()
        {
            Assert.That(ToggleKey.ShouldType(null), Is.False);
        }

        [Test] public void ShouldType_NonTextElement_False()
        {
            Assert.That(ToggleKey.ShouldType(new Button()), Is.False);
        }

        [Test] public void ShouldType_EmptyTextField_False()
        {
            Assert.That(ToggleKey.ShouldType(new TextField()), Is.False);
        }

        [Test] public void ShouldType_TextFieldWithText_True()
        {
            Assert.That(ToggleKey.ShouldType(new TextField { value = "Assets" }), Is.True);
        }

        [Test] public void ShouldType_ElementInsideTextFieldWithText_True()
        {
            var field = new TextField { value = "Assets" };
            var inner = field.Q("unity-text-input");

            Assert.That(ToggleKey.ShouldType(inner), Is.True);
        }

        [Test] public void IsEditingText_EmptyTextField_True()
        {
            Assert.That(ToggleKey.IsEditingText(new TextField()), Is.True);
        }

        [Test] public void IsEditingText_NonTextElement_False()
        {
            Assert.That(ToggleKey.IsEditingText(new Label()), Is.False);
        }
    }
}
