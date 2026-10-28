using System.Collections.Generic;
using System.Linq;
using Bossy.Frontend.Parsing;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="Tokenizer"/> class.
    /// </summary>
    internal class TokenizerTest
    {
        private static readonly List<string> Ops = new() { "|", "||", "&&", ";" };
        
        // Basic splitting
        [Test] public void SingleWord() =>
            Assert.That(Texts("hello"), Is.EqualTo(new[] { "hello" }));

        [Test] public void MultipleWords() =>
            Assert.That(Texts("foo bar baz"), Is.EqualTo(new[] { "foo", "bar", "baz" }));

        [Test] public void LeadingTrailingWhitespace() =>
            Assert.That(Texts("  foo bar  "), Is.EqualTo(new[] { "foo", "bar" }));

        [Test] public void EmptyString() =>
            Assert.That(Texts(""), Is.Empty);

        [Test] public void OnlyWhitespace() =>
            Assert.That(Texts("   "), Is.Empty);

        // Double quotes
        [Test] public void DoubleQuotedPhrase() =>
            Assert.That(Texts("\"hello world\""), Is.EqualTo(new[] { "hello world" }));

        [Test] public void DoubleQuotedPhraseAmongTokens() =>
            Assert.That(Texts("foo \"hello world\" bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        [Test] public void EmptyDoubleQuotes() =>
            Assert.That(Texts("\"\""), Is.EqualTo(new[] { "" }));

        // Single quotes
        [Test] public void SingleQuotedPhrase() =>
            Assert.That(Texts("'hello world'"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void SingleQuotedAmongTokens() =>
            Assert.That(Texts("foo 'hello world' bar"), Is.EqualTo(new[] { "foo", "hello world", "bar" }));

        // Adjacent quoting (concatenation - bash standard)
        [Test] public void AdjacentQuoteAndWord() =>
            Assert.That(Texts("hello\"world\""), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void WordAdjacentToQuote() =>
            Assert.That(Texts("\"hello\"world"), Is.EqualTo(new[] { "helloworld" }));

        [Test] public void TwoAdjacentQuotedStrings() =>
            Assert.That(Texts("\"foo\"\"bar\""), Is.EqualTo(new[] { "foobar" }));

        [Test] public void EmbeddedSingleQuoteViaDoubleQuotes() =>
            Assert.That(Texts("--name foo\"'\"s"), Is.EqualTo(new[] { "--name", "foo's" }));

        // Escape sequences
        [Test] public void BackslashEscapedSpace() =>
            Assert.That(Texts("hello\\ world"), Is.EqualTo(new[] { "hello world" }));

        [Test] public void EscapedQuoteInsideDoubleQuotes() =>
            Assert.That(Texts("\"she said \\\"hi\\\"\""), Is.EqualTo(new[] { "she said \"hi\"" }));

        [Test] public void EscapedBackslash() =>
            Assert.That(Texts("foo\\\\bar"), Is.EqualTo(new[] { "foo\\bar" }));

        [Test] public void EscapedCharOutsideQuotes() =>
            Assert.That(Texts("foo\\nbar"), Is.EqualTo(new[] { "foonbar" }));
        
        // Real command-line style inputs
        [Test] public void FlagWithValue() =>
            Assert.That(Texts("--name \"John Doe\""), Is.EqualTo(new[] { "--name", "John Doe" }));

        [Test] public void Vector3Style() =>
            Assert.That(Texts("--position 1 2 3"), Is.EqualTo(new[] { "--position", "1", "2", "3" }));

        [Test] public void MixedFlagsAndPositionals() =>
            Assert.That(Texts("spawn --name \"Enemy One\" 1 2 3"), Is.EqualTo(new[] { "spawn", "--name", "Enemy One", "1", "2", "3" }));
        
        // Operator splitting - basic
        [Test] public void PipeOperator() =>
            Assert.That(Texts("cmd1|cmd2", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2" }));

        [Test] public void PipeWithSpaces() =>
            Assert.That(Texts("cmd1 | cmd2", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2" }));

        [Test] public void DoublePipeDistinctFromSingle() =>
            Assert.That(Texts("cmd1||cmd2", Ops), Is.EqualTo(new[] { "cmd1", "||", "cmd2" }));

        [Test] public void ChainedOperators() =>
            Assert.That(Texts("cmd1 | cmd2 && cmd3", Ops), Is.EqualTo(new[] { "cmd1", "|", "cmd2", "&&", "cmd3" }));

        // Operators inside quotes are not split
        [Test] public void PipeInsideDoubleQuotesIgnored() =>
            Assert.That(Texts("cmd \"foo|bar\"", Ops), Is.EqualTo(new[] { "cmd", "foo|bar" }));

        [Test] public void PipeInsideSingleQuotesIgnored() =>
            Assert.That(Texts("cmd 'foo||bar'", Ops), Is.EqualTo(new[] { "cmd", "foo||bar" }));

        // Operators with args on either side
        [Test] public void PipeWithFlags() =>
            Assert.That(Texts("spawn --name \"Enemy One\" | log", Ops), Is.EqualTo(new[] { "spawn", "--name", "Enemy One", "|", "log" }));
        
        // Operators with args on either side
        [Test] public void PreferLongerOperators() =>
            Assert.That(Texts("cmd1|||cmd2", Ops), Is.EqualTo(new[] { "cmd1", "||", "|", "cmd2" }));

        // Operator marking
        [Test]
        public void Tokenize_MarksUnquotedOperators()
        {
            var tokens = Tokenizer.Tokenize("cmd1 | cmd2 && cmd3", Ops);

            Assert.That(tokens.Select(t => t.Text), Is.EqualTo(new[] { "cmd1", "|", "cmd2", "&&", "cmd3" }));
            Assert.That(tokens.Select(t => t.IsOperator), Is.EqualTo(new[] { false, true, false, true, false }));
        }

        [Test]
        public void Tokenize_QuotedOperatorIsNotOperator()
        {
            var tokens = Tokenizer.Tokenize("cmd \"|\" '&&' \"!\"", new List<string>(Ops) { "!" });

            Assert.That(tokens.Select(t => t.Text), Is.EqualTo(new[] { "cmd", "|", "&&", "!" }));
            Assert.That(tokens.Any(t => t.IsOperator), Is.False);
        }

        // Quote marking
        [Test]
        public void Tokenize_MarksQuotedAndEscapedTokens()
        {
            var tokens = Tokenizer.Tokenize("cmd -a \"-b\" '-c' pre\"fix\" \\-d", Ops);

            Assert.That(tokens.Select(t => t.Text), Is.EqualTo(new[] { "cmd", "-a", "-b", "-c", "prefix", "-d" }));
            Assert.That(tokens.Select(t => t.IsQuoted), Is.EqualTo(new[] { false, false, true, true, true, true }));
        }

        // Spans
        [Test]
        public void Tokenize_RecordsSpansIncludingQuotes()
        {
            var tokens = Tokenizer.Tokenize("load \"My Scene\" &&x", Ops);

            Assert.That(tokens.Select(t => (t.Start, t.End)), Is.EqualTo(new[] { (0, 4), (5, 15), (16, 18), (18, 19) }));
        }

        // Unterminated quotes
        [Test]
        public void Tokenize_UnterminatedQuoteRunsToEndOfSegment()
        {
            var tokens = Tokenizer.Tokenize("load \"My Sc && x", Ops);

            Assert.That(tokens.Select(t => t.Text), Is.EqualTo(new[] { "load", "My Sc && x" }));
            Assert.That(tokens[1].IsQuoted, Is.True);
            Assert.That(tokens[1].End, Is.EqualTo(16));
        }

        // Switch detection
        [Test]
        public void IsSwitch_RejectsQuotedTextNegativeNumbersAndOperators()
        {
            var tokens = Tokenizer.Tokenize("-a --b \"-c\" -5 -.5 - |", Ops);

            Assert.That(tokens.Select(t => t.IsSwitch), Is.EqualTo(new[] { true, true, false, false, false, true, false }));
        }

        private static List<string> Texts(string line, List<string> operators = null)
        {
            return Tokenizer.Tokenize(line, operators).Select(t => t.Text).ToList();
        }
    }
}