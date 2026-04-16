using System.Collections.Generic;
using Bossy.FrontEnd.Parsing;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="Tokenizer"/> class.
    /// </summary>
    public class TokenizerTest
    {
        private static readonly List<string> Ops = new() { "|", "||", "&&", ";" };
        
        // Basic splitting
        [Test] public void SingleWord() =>
            Assert.That(Tokenizer.Tokenize("hello"), Is.EqualTo(new[] { "hello" }));

        [Test] public void MultipleWords() =>
            Assert.That(Tokenizer.Tokenize("foo bar baz"), Is.EqualTo(new[] { "foo", "bar", "baz" }));

        [Test] public void LeadingTrailingWhitespace() =>
            Assert.That(Tokenizer.Tokenize("  foo bar  "), Is.EqualTo(new[] { "foo", "bar" }));

        [Test] public void EmptyString() =>
            Assert.That(Tokenizer.Tokenize(""), Is.Empty);

        [Test] public void OnlyWhitespace() =>
            Assert.That(Tokenizer.Tokenize("   "), Is.Empty);

        // Double quotes
        [Test] public void DoubleQuotedPhrase() =>
            Assert.That(Tokenizer.Tokenize("\"hello world\""), Is.EqualTo(new[] { "hello world" }));

        [Test] public void DoubleQuotedPhraseAmongTokens() =>
            Assert.That(Tokenizer.Tokenize("foo \"hello world\" bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        [Test] public void EmptyDoubleQuotes() =>
            Assert.That(Tokenizer.Tokenize("\"\""), Is.EqualTo(new[] { "" }));

        // Single quotes
        [Test] public void SingleQuotedPhrase() =>
            Assert.That(Tokenizer.Tokenize("'hello world'"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void SingleQuotedAmongTokens() =>
            Assert.That(Tokenizer.Tokenize("foo 'hello world' bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        // Adjacent quoting (concatenation - bash standard)
        [Test] public void AdjacentQuoteAndWord() =>
            Assert.That(Tokenizer.Tokenize("hello\"world\""), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void WordAdjacentToQuote() =>
            Assert.That(Tokenizer.Tokenize("\"hello\"world"), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void TwoAdjacentQuotedStrings() =>
            Assert.That(Tokenizer.Tokenize("\"foo\"\"bar\""), Is.EqualTo(new[] { "foobar" }));

        [Test] public void EmbeddedSingleQuoteViaDoubleQuotes() =>
            Assert.That(Tokenizer.Tokenize("--name foo\"'\"s"), Is.EqualTo(new[] { "--name", "foo's" }));

        // Escape sequences
        [Test] public void BackslashEscapedSpace() =>
            Assert.That(Tokenizer.Tokenize("hello\\ world"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void EscapedQuoteInsideDoubleQuotes() =>
            Assert.That(Tokenizer.Tokenize("\"she said \\\"hi\\\"\""), Is.EqualTo(new[] { "she said \"hi\"" }));

        [Test] public void EscapedBackslash() =>
            Assert.That(Tokenizer.Tokenize("foo\\\\bar"), Is.EqualTo(new[] { "foo\\bar" }));

        [Test] public void EscapedCharOutsideQuotes() =>
            Assert.That(Tokenizer.Tokenize("foo\\nbar"), Is.EqualTo(new[] { "foonbar" }));
        
        // Real command-line style inputs
        [Test] public void FlagWithValue() =>
            Assert.That(Tokenizer.Tokenize("--name \"John Doe\""), Is.EqualTo(new[] { "--name", "John Doe" }));

        [Test] public void Vector3Style() =>
            Assert.That(Tokenizer.Tokenize("--position 1 2 3"), Is.EqualTo(new[] { "--position", "1", "2", "3" }));

        [Test] public void MixedFlagsAndPositionals() =>
            Assert.That(Tokenizer.Tokenize("spawn --name \"Enemy One\" 1 2 3"), Is.EqualTo(new[] { "spawn", "--name", "Enemy One", "1", "2", "3" }));
        
        // Operator splitting - basic
        [Test] public void PipeOperator() =>
            Assert.That(Tokenizer.Tokenize("cmd1|cmd2", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2" }));

        [Test] public void PipeWithSpaces() =>
            Assert.That(Tokenizer.Tokenize("cmd1 | cmd2", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2" }));

        [Test] public void DoublePipeDistinctFromSingle() =>
            Assert.That(Tokenizer.Tokenize("cmd1||cmd2", Ops), Is.EqualTo(new[] { "cmd1", "||", "cmd2" }));

        [Test] public void ChainedOperators() =>
            Assert.That(Tokenizer.Tokenize("cmd1 | cmd2 && cmd3", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2", "&&", "cmd3" }));

        // Operators inside quotes are not split
        [Test] public void PipeInsideDoubleQuotesIgnored() =>
            Assert.That(Tokenizer.Tokenize("cmd \"foo|bar\"", Ops), Is.EqualTo(new[] { "cmd", "foo|bar" }));

        [Test] public void PipeInsideSingleQuotesIgnored() =>
            Assert.That(Tokenizer.Tokenize("cmd 'foo||bar'", Ops), Is.EqualTo(new[] { "cmd", "foo||bar" }));

        // Operators with args on either side
        [Test] public void PipeWithFlags() =>
            Assert.That(Tokenizer.Tokenize("spawn --name \"Enemy One\" | log", Ops), Is.EqualTo(new[] { "spawn", "--name", "Enemy One", "|", "log" }));
        
        // Operators with args on either side
        [Test] public void PreferLongerOperators() =>
            Assert.That(Tokenizer.Tokenize("cmd1|||cmd2", Ops), Is.EqualTo(new[] { "cmd1", "||", "|", "cmd2" }));
    }
}