using Bossy.FrontEnd.Parsing;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="TokenStream"/> class.
    /// </summary>
    public class TokenStreamTest
    {
        // Cursor mechanics
        [Test] public void TryConsumeReturnsFalseWhenEmpty()
        {
            var cursor = new TokenStream("");
            Assert.That(cursor.TryConsume(out _), Is.False);
        }

        [Test] public void TryConsumeMultiple()
        {
            var cursor = new TokenStream("1 2 3");
            Assert.That(cursor.TryConsume(3, out var tokens), Is.True);
            Assert.That(tokens, Is.EqualTo(new[] { "1", "2", "3" }));
        }

        [Test] public void TryConsumeMultipleFailsIfNotEnough()
        {
            var cursor = new TokenStream("1 2");
            Assert.That(cursor.TryConsume(3, out var tokens), Is.False);
            Assert.That(tokens, Is.Empty);
        }

        [Test] public void TryConsumeMultipleDoesNotAdvanceCursorOnFailure()
        {
            var cursor = new TokenStream("1 2");
            cursor.TryConsume(3, out _);
            Assert.That(cursor.TryConsume(out var token), Is.True);
            Assert.That(token, Is.EqualTo("1"));
        }

        [Test] public void PartialConsumesThenExhausted()
        {
            var cursor = new TokenStream("a b c");
            cursor.TryConsume(out _);
            cursor.TryConsume(out _);
            cursor.TryConsume(out _);
            Assert.That(cursor.TryConsume(out _), Is.False);
        }
        
        [Test] public void TryPeek()
        {
            var cursor = new TokenStream("a");
            Assert.That(cursor.TryPeek(out var token), Is.True);
            Assert.That(token, Is.EqualTo("a"));
            Assert.That(cursor.TryPeek(out token), Is.True);
            Assert.That(token, Is.EqualTo("a"));

            cursor.TryConsume(out _);
            
            Assert.False(cursor.TryPeek(out _));
            Assert.False(cursor.TryPeek(out _));
        }
    }
}