using Bossy.Frontend;
using Bossy.Frontend.Autocomplete;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Views
{
    /// <summary>
    /// Tests the <see cref="CompletionList"/> class.
    /// </summary>
    internal class CompletionListTest
    {
        [Test]
        public void New_HighlightsFirstRow()
        {
            var list = MakeList("lo", 2, Value(0, 2, "load"), Value(0, 2, "lock"));

            Assert.That(list.HasRows, Is.True);
            Assert.That(list.Highlighted, Is.EqualTo(0));
        }

        [Test]
        public void Move_WrapsAroundBothEnds()
        {
            var list = MakeList("lo", 2, Value(0, 2, "load"), Value(0, 2, "lock"), Value(0, 2, "log"));

            list.Move(-1);
            Assert.That(list.Highlighted, Is.EqualTo(2));

            list.Move(1);
            Assert.That(list.Highlighted, Is.EqualTo(0));
        }

        [Test]
        public void TryAccept_NoRows_ReturnsFalse()
        {
            var list = new CompletionList(CompletionResult.Hint("scene <string>"), "load ", 5);

            Assert.That(list.HasRows, Is.False);
            Assert.That(list.TryAccept(out _, out _), Is.False);
        }

        [Test]
        public void TryAccept_AtEndOfLine_AppliesHighlightedAndAddsSpace()
        {
            var list = MakeList("lo", 2, Value(0, 2, "load"), Value(0, 2, "lock"));
            list.Move(1);

            Assert.That(list.TryAccept(out var line, out var cursor), Is.True);
            Assert.That(line, Is.EqualTo("lock "));
            Assert.That(cursor, Is.EqualTo(5));
        }

        [Test]
        public void TryAccept_BeforeSpace_MovesPastItWithoutAddingAnother()
        {
            var list = MakeList("load Lev extra", 8, Value(5, 8, "Level1"));

            list.TryAccept(out var line, out var cursor);

            Assert.That(line, Is.EqualTo("load Level1 extra"));
            Assert.That(cursor, Is.EqualTo("load Level1 ".Length));
        }

        [Test]
        public void TryAccept_InsertTextEndsInSpace_AddsNoSpace()
        {
            var list = MakeList("load -f", 5, Value(5, 5, "Level1 "));

            list.TryAccept(out var line, out var cursor);

            Assert.That(line, Is.EqualTo("load Level1 -f"));
            Assert.That(cursor, Is.EqualTo("load Level1 ".Length));
        }

        [Test]
        public void TryAccept_SpanBeforeCursor_ShiftsCursorWithoutAddingSpace()
        {
            var list = MakeList("laod Lev", 8, Value(0, 4, "loader"));

            list.TryAccept(out var line, out var cursor);

            Assert.That(line, Is.EqualTo("loader Lev"));
            Assert.That(cursor, Is.EqualTo("loader Lev".Length));
        }

        private static CompletionList MakeList(string line, int cursor, params Completion[] completions)
        {
            return new CompletionList(CompletionResult.List(completions), line, cursor);
        }

        private static Completion Value(int start, int end, string insertText)
        {
            return new Completion(start, end, insertText, insertText.Trim(), string.Empty, CompletionKind.Value);
        }
    }
}
