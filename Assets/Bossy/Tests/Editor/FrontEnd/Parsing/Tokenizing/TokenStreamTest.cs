using Bossy.FrontEnd.Parsing;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="TokenStream"/> class.
    /// </summary>
    internal class TokenStreamTest
    {
        // Stream mechanics
        [Test] public void TryConsumeReturnsFalseWhenEmpty()
        {
            var stream = new TokenStream("");
            Assert.That(stream.TryConsume(out _), Is.False);
        }

        [Test] public void TryConsumeMultiple()
        {
            var stream = new TokenStream("1 2 3");
            Assert.That(stream.TryConsume(3, out var tokens), Is.True);
            Assert.That(tokens, Is.EqualTo(new[] { "1", "2", "3" }));
        }

        [Test] public void TryConsumeMultipleFailsIfNotEnough()
        {
            var stream = new TokenStream("1 2");
            Assert.That(stream.TryConsume(3, out var tokens), Is.False);
            Assert.That(tokens, Is.Empty);
        }

        [Test] public void TryConsumeMultipleDoesNotAdvanceStreamOnFailure()
        {
            var stream = new TokenStream("1 2");
            stream.TryConsume(3, out _);
            Assert.That(stream.TryConsume(out var token), Is.True);
            Assert.That(token, Is.EqualTo("1"));
        }

        [Test] public void PartialConsumesThenExhausted()
        {
            var stream = new TokenStream("a b c");
            stream.TryConsume(out _);
            stream.TryConsume(out _);
            stream.TryConsume(out _);
            Assert.That(stream.TryConsume(out _), Is.False);
        }
        
        [Test] public void TryPeek()
        {
            var stream = new TokenStream("a");
            Assert.That(stream.TryPeek(out var token), Is.True);
            Assert.That(token, Is.EqualTo("a"));
            Assert.That(stream.TryPeek(out token), Is.True);
            Assert.That(token, Is.EqualTo("a"));

            stream.TryConsume(out _);
            
            Assert.False(stream.TryPeek(out _));
            Assert.False(stream.TryPeek(out _));
        }
        
        [Test] public void Explode()
        {
            var stream = new TokenStream("a b c");
            
            stream.TryConsume(out _);

            Assert.That(stream.Explode(), Is.EquivalentTo(new[] { "b", "c" }));
        }
    }
}