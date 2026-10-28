using Bossy.Command;
using Bossy.Frontend;
using Bossy.Frontend.Autocomplete;
using Bossy.Schema;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Views
{
    /// <summary>
    /// Tests the text helpers of the <see cref="CompletionPopup"/> class.
    /// </summary>
    internal class CompletionPopupTest
    {
        [Test]
        public void HighlightMatches_NoMatches_KeepsTextLiteral()
        {
            var text = CompletionPopup.HighlightMatches("--count <int>", new MatchRange[0]);

            Assert.That(text, Is.EqualTo("<noparse>--count <int></noparse>"));
        }

        [Test]
        public void HighlightMatches_WrapsEachRun()
        {
            var text = CompletionPopup.HighlightMatches("SetActive", new[] { new MatchRange(0, 1), new MatchRange(3, 1) });

            var s = Highlight("S");
            var a = Highlight("A");
            Assert.That(text, Is.EqualTo($"{s}<noparse>et</noparse>{a}<noparse>ctive</noparse>"));
        }

        [Test]
        public void HighlightMatches_SkipsRangesOutsideText()
        {
            var text = CompletionPopup.HighlightMatches("load", new[] { new MatchRange(2, 5) });

            Assert.That(text, Is.EqualTo("<noparse>load</noparse>"));
        }

        [Test]
        public void FailureText_NamesArgumentAndReason()
        {
            var argument = new ArgumentSchema("scene", "", null, new PositionalAttribute(0, ""), null);

            var text = CompletionPopup.FailureText(new SuggestionFailure(argument, "No <assets>"));

            Assert.That(text, Is.EqualTo($"Suggestions for {Format.Bold("<noparse>scene</noparse>")} failed: <noparse>No <assets></noparse>"));
        }

        [Test]
        public void KindTag_TagsEveryKindButPositional()
        {
            Assert.That(CompletionPopup.KindTag(new PositionalAttribute(0, "")), Is.Null);
            Assert.That(CompletionPopup.KindTag(new OptionalAttribute(0, "")), Is.EqualTo("opt"));
            Assert.That(CompletionPopup.KindTag(new SwitchAttribute('s', "")), Is.EqualTo("switch"));
            Assert.That(CompletionPopup.KindTag(new VariadicAttribute("")), Is.EqualTo("var"));
        }

        private static string Highlight(string text)
        {
            return Format.Bold(Format.Color($"<noparse>{text}</noparse>", Format.Accent));
        }
    }
}
