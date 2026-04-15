using System.Collections.Generic;
using Bossy.FrontEnd.Parsing;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="TokenStream"/> class.
    /// </summary>
    public class TokenStreamTest
    {
        private static List<string> Tokenize(string input)
        {
            var cursor = new TokenStream(input);
            var tokens = new List<string>();
            while (cursor.TryConsume(out var token))
                tokens.Add(token);
            return tokens;
        }

        // Basic splitting
        [Test] public void SingleWord() =>
            Assert.That(Tokenize("hello"), Is.EqualTo(new[] { "hello" }));

        [Test] public void MultipleWords() =>
            Assert.That(Tokenize("foo bar baz"), Is.EqualTo(new[] { "foo", "bar", "baz" }));

        [Test] public void LeadingTrailingWhitespace() =>
            Assert.That(Tokenize("  foo bar  "), Is.EqualTo(new[] { "foo", "bar" }));

        [Test] public void EmptyString() =>
            Assert.That(Tokenize(""), Is.Empty);

        [Test] public void OnlyWhitespace() =>
            Assert.That(Tokenize("   "), Is.Empty);

        // Double quotes
        [Test] public void DoubleQuotedPhrase() =>
            Assert.That(Tokenize("\"hello world\""), Is.EqualTo(new[] { "hello world" }));

        [Test] public void DoubleQuotedPhraseAmongTokens() =>
            Assert.That(Tokenize("foo \"hello world\" bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        [Test] public void EmptyDoubleQuotes() =>
            Assert.That(Tokenize("\"\""), Is.EqualTo(new[] { "" }));

        // Single quotes
        [Test] public void SingleQuotedPhrase() =>
            Assert.That(Tokenize("'hello world'"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void SingleQuotedAmongTokens() =>
            Assert.That(Tokenize("foo 'hello world' bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        // Adjacent quoting (concatenation - bash standard)
        [Test] public void AdjacentQuoteAndWord() =>
            Assert.That(Tokenize("hello\"world\""), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void WordAdjacentToQuote() =>
            Assert.That(Tokenize("\"hello\"world"), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void TwoAdjacentQuotedStrings() =>
            Assert.That(Tokenize("\"foo\"\"bar\""), Is.EqualTo(new[] { "foobar" }));

        [Test] public void EmbeddedSingleQuoteViaDoubleQuotes() =>
            Assert.That(Tokenize("--name foo\"'\"s"), Is.EqualTo(new[] { "--name", "foo's" }));

        // Escape sequences
        [Test] public void BackslashEscapedSpace() =>
            Assert.That(Tokenize("hello\\ world"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void EscapedQuoteInsideDoubleQuotes() =>
            Assert.That(Tokenize("\"she said \\\"hi\\\"\""), Is.EqualTo(new[] { "she said \"hi\"" }));

        [Test] public void EscapedBackslash() =>
            Assert.That(Tokenize("foo\\\\bar"), Is.EqualTo(new[] { "foo\\bar" }));

        [Test] public void EscapedCharOutsideQuotes() =>
            Assert.That(Tokenize("foo\\nbar"), Is.EqualTo(new[] { "foonbar" }));

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

        // Real command-line style inputs
        [Test] public void FlagWithValue() =>
            Assert.That(Tokenize("--name \"John Doe\""), Is.EqualTo(new[] { "--name", "John Doe" }));

        [Test] public void Vector3Style() =>
            Assert.That(Tokenize("--position 1 2 3"), Is.EqualTo(new[] { "--position", "1", "2", "3" }));

        [Test] public void MixedFlagsAndPositionals() =>
            Assert.That(Tokenize("spawn --name \"Enemy One\" 1 2 3"), Is.EqualTo(new[] { "spawn", "--name", "Enemy One", "1", "2", "3" }));
    }
}