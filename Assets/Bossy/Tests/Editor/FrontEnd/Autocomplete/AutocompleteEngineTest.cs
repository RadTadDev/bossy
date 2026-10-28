using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Frontend.Autocomplete;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Tests.Utils;
using NUnit.Framework;
using UnityEngine;

namespace Bossy.Tests.FrontEnd.Autocomplete
{
    /// <summary>
    /// Characterization tests for the <see cref="AutocompleteEngine"/>. These pin the engine's current behavior so
    /// that it can be rewritten safely. Tests marked "Current behavior, scheduled to change" pin known bugs that a
    /// later change fixes on purpose, at which point the test is updated with it.
    /// </summary>
    internal class AutocompleteEngineTest
    {
        private AutocompleteEngine _engine;

        [OneTimeSetUp]
        public void Setup()
        {
            var load = CommandGenerator.WithName("load")
                .WithPositional("scene", typeof(string)).WithSuggestions(nameof(TestSuggestions.Scenes), typeof(TestSuggestions))
                .WithSwitch("force", typeof(bool), 'f')
                .WithSwitch("verbose", typeof(bool), 'v')
                .WithSwitch("count", typeof(int), 'c')
                .Generate().GetType();

            var wait = CommandGenerator.WithName("wait")
                .WithPositional("seconds", typeof(float))
                .Generate().GetType();

            var vec = CommandGenerator.WithName("vec")
                .WithPositional("pos", typeof(Vector3))
                .Generate().GetType();

            var tag = CommandGenerator.WithName("tag")
                .WithOptional("amount", typeof(int))
                .WithVariadic("words", typeof(string))
                .Generate().GetType();

            var pick = CommandGenerator.WithName("pick")
                .WithPositional("item", typeof(string)).WithSuggestions(nameof(TestSuggestions.Many), typeof(TestSuggestions))
                .Generate().GetType();

            var level = CommandGenerator.WithName("level")
                .WithOptional("name", typeof(string))
                .Generate().GetType();

            var levelOpen = CommandGenerator.WithName("open")
                .AsSubcommand(level)
                .WithPositional("path", typeof(string))
                .Generate().GetType();

            var levelClose = CommandGenerator.WithName("close")
                .AsSubcommand(level)
                .Generate().GetType();

            var types = new List<Type> { load, wait, vec, tag, pick, level, levelOpen, levelClose };
            var graph = CommandDependencyGraphBuilder.BuildGraph(types);
            var schemas = new SchemaRegistry(SchemaFactory.BuildCommandSchemas(graph));

            var adapters = new TypeAdapterRegistry();
            adapters.RegisterAdapter(new BoolAdapter());
            adapters.RegisterAdapter(new IntAdapter());
            adapters.RegisterAdapter(new FloatAdapter());
            adapters.RegisterAdapter(new StringAdapter());
            adapters.RegisterAdapter(new Vector3Adapter());

            var context = new BossyContext(schemas, adapters, null, null, null, null);
            var operators = new OperatorList(";", "&&", "||", "|", "!");

            _engine = new AutocompleteEngine(context, operators);
        }

        // --- Input and cursor ---

        [Test]
        public void Suggest_EmptyInput_ReturnsNothing()
        {
            Assert.That(Suggest(""), Is.Empty);
        }

        [Test]
        public void Suggest_CursorBeforeEnd_IgnoresTextAfterCursor()
        {
            var result = _engine.Suggest("load Lev extra", 8).ToList();

            Assert.That(FullTexts(result)[0], Is.EqualTo("load Level1"));
        }

        // --- Command names ---

        [Test]
        public void Suggest_TypingCommandName_SuggestsMatchingCommands()
        {
            var result = Suggest("lo");

            Assert.That(FullTexts(result)[0], Is.EqualTo("load"));
        }

        [Test]
        public void Suggest_NoCommandMatches_ReturnsNothingForWholeLine()
        {
            Assert.That(Suggest("xyzq"), Is.Empty);
            Assert.That(Suggest("xyzq "), Is.Empty);
            Assert.That(Suggest("xyzq -f "), Is.Empty);
        }

        [Test]
        public void Suggest_MisspelledCommand_ResolvesToBestMatchAndCorrectsIt()
        {
            var result = Suggest("laod ");

            Assert.That(FullTexts(result), Is.EqualTo(new[] { "load \"Main Menu\"", "load Level1", "load Level2" }));
        }

        // --- Operators ---

        [Test]
        public void Suggest_AfterOperator_SuggestsNextCommandKeepingPrefix()
        {
            var result = Suggest("wait 2 && lo");

            Assert.That(FullTexts(result)[0], Is.EqualTo("wait 2 && load"));
        }

        [Test]
        public void Suggest_OperatorAfterInvalidValue_ReturnsError()
        {
            AssertError(Suggest("wait abc && lo"), "Invalid input");
        }

        [Test]
        public void Suggest_OperatorAfterIncompleteCommand_SuggestsNextCommand()
        {
            // Current behavior, scheduled to change: the missing positional should be reported as an error
            var result = Suggest("wait && lo");

            Assert.That(FullTexts(result)[0], Is.EqualTo("wait && load"));
        }

        [Test]
        public void Suggest_TrailingWindowOperator_IsReadyToExecute()
        {
            AssertHint(Suggest("wait 2 !"), "Ready to execute");
        }

        [Test]
        public void Suggest_WindowOperatorAfterIncompleteCommand_IsReadyToExecute()
        {
            // Current behavior, scheduled to change: the missing positional should be reported as an error
            AssertHint(Suggest("wait !"), "Ready to execute");
        }

        [Test]
        public void Suggest_LeadingWindowOperator_ReturnsNothing()
        {
            // Current behavior, scheduled to change: the leading operator should be skipped
            Assert.That(Suggest("!wait"), Is.Empty);
        }

        [Test]
        public void Suggest_QuotedWindowOperator_IsTreatedAsOperator()
        {
            // Current behavior, scheduled to change: a quoted operator should be a plain value
            AssertHint(Suggest("load \"!\" "), "Ready to execute");
        }

        // --- Quotes ---

        [Test]
        public void Suggest_UnfinishedQuote_HintsCurrentArgument()
        {
            AssertHint(Suggest("load \"Main"), "scene");
        }

        [Test]
        public void Suggest_UnfinishedQuoteWithNoArgumentLeft_ReturnsError()
        {
            AssertError(Suggest("level close \"x"), "Invalid token");
        }

        [Test]
        public void Suggest_AfterQuotedValue_DropsQuotesFromSuggestedLine()
        {
            // Current behavior, scheduled to change: the user's quotes should be kept
            var result = Suggest("load \"Main Menu\" -");

            Assert.That(FullTexts(result), Is.EquivalentTo(new[]
            {
                "load Main Menu --force",
                "load Main Menu --verbose",
                "load Main Menu --count"
            }));
        }

        // --- Long switches ---

        [Test]
        public void Suggest_DoubleDash_SuggestsAllLongSwitches()
        {
            var result = Suggest("load --");

            Assert.That(FullTexts(result), Is.EquivalentTo(new[] { "load --force", "load --verbose", "load --count" }));
        }

        [Test]
        public void Suggest_PartialLongSwitch_SuggestsMatchingSwitchFirst()
        {
            var result = Suggest("load --ver");

            Assert.That(FullTexts(result)[0], Is.EqualTo("load --verbose"));
        }

        [Test]
        public void Suggest_TokensAfterSwitchSurrender_AreSkipped()
        {
            // Current behavior, scheduled to change: "-x" should fill the scene positional, as it does in the parser
            var result = Suggest("load -- -x ");

            Assert.That(FullTexts(result), Does.Contain("load -- -x Level1"));
        }

        [Test]
        public void Suggest_UnknownSwitch_ReturnsError()
        {
            AssertError(Suggest("load --nope "), "Unrecognized switch arg");
        }

        // --- Short switches ---

        [Test]
        public void Suggest_SingleDash_SuggestsLongSwitches()
        {
            var result = Suggest("load -");

            Assert.That(FullTexts(result), Is.EquivalentTo(new[] { "load --force", "load --verbose", "load --count" }));
        }

        [Test]
        public void Suggest_SingleDashMovedOn_ReturnsError()
        {
            AssertError(Suggest("load - "), "'-' is not a valid token");
        }

        [Test]
        public void Suggest_ShortSwitch_SuggestsShortNamesWithMatchFirst()
        {
            var result = Suggest("load -v");

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(FullTexts(result)[0], Is.EqualTo("load -v"));
        }

        [Test]
        public void Suggest_AggregateBoolSwitches_Hints()
        {
            AssertHint(Suggest("load -fv"), "Aggregate flags");
        }

        [Test]
        public void Suggest_AggregateBoolSwitchesMovedOn_ContinuesToNextArgument()
        {
            var result = Suggest("load -fv ");

            Assert.That(FullTexts(result), Does.Contain("load -fv Level1"));
        }

        [Test]
        public void Suggest_AggregateWithNonBoolSwitch_ReturnsError()
        {
            AssertError(Suggest("load -fc"), "Unrecognized boolean switch");
        }

        [Test]
        public void Suggest_SingleDashLongSwitchPrefix_SuggestsLongSwitch()
        {
            var result = Suggest("load -verb");

            Assert.That(FullTexts(result), Is.EqualTo(new[] { "load --verbose" }));
        }

        // --- Switch values ---

        [Test]
        public void Suggest_AfterBoolSwitch_ContinuesToNextArgument()
        {
            var result = Suggest("load --force ");

            Assert.That(FullTexts(result), Does.Contain("load --force Level1"));
        }

        [Test]
        public void Suggest_AfterValueSwitch_HintsItsType()
        {
            AssertHint(Suggest("load --count "), "count");
        }

        [Test]
        public void Suggest_InvalidSwitchValueMovedOn_ReturnsError()
        {
            AssertError(Suggest("load --count abc "), "is invalid for arg --count");
        }

        [Test]
        public void Suggest_InvalidSwitchValueAtCursor_HintsInvalidValue()
        {
            AssertHint(Suggest("load --count abc"), "current value 'abc' is invalid");
        }

        [Test]
        public void Suggest_ValidSwitchValueMovedOn_ContinuesToNextArgument()
        {
            var result = Suggest("load --count 5 ");

            Assert.That(FullTexts(result), Does.Contain("load --count 5 Level1"));
        }

        // --- Ordered values ---

        [Test]
        public void Suggest_NextPositional_SuggestsUserValuesInOrderAndQuotesWhitespace()
        {
            var result = Suggest("load ");

            Assert.That(FullTexts(result), Is.EqualTo(new[] { "load \"Main Menu\"", "load Level1", "load Level2" }));
            Assert.That(result[1].DisplayText, Does.Contain("scene").And.Contain("Level1"));
        }

        [Test]
        public void Suggest_PartialPositional_RanksPrefixMatchesFirst()
        {
            var result = Suggest("load Lev");

            Assert.That(FullTexts(result)[0], Is.EqualTo("load Level1"));
            Assert.That(FullTexts(result)[1], Is.EqualTo("load Level2"));
        }

        [Test]
        public void Suggest_NextPositionalWithoutSuggestions_HintsItsType()
        {
            AssertHint(Suggest("wait "), "seconds");
        }

        [Test]
        public void Suggest_InvalidPositionalMovedOn_ReturnsError()
        {
            AssertError(Suggest("wait abc "), "Invalid input for arg seconds");
        }

        [Test]
        public void Suggest_InvalidPositionalAtCursor_HintsInvalidValue()
        {
            AssertHint(Suggest("wait abc"), "current value 'abc' is invalid");
        }

        [Test]
        public void Suggest_MultiTokenValueWithTooFewTokens_HintsTypeWithoutError()
        {
            var result = Suggest("vec 1 2");

            AssertHint(result, "pos");
            Assert.That(result[0].DisplayText, Does.Not.Contain("invalid"));
        }

        [Test]
        public void Suggest_MultiTokenValueInvalid_HintsInvalidValue()
        {
            AssertHint(Suggest("vec 1 2 x"), "current value '1 2 x' is invalid");
        }

        [Test]
        public void Suggest_AllArgumentsFilled_IsReadyToExecute()
        {
            AssertHint(Suggest("wait 2 "), "Ready to execute");
        }

        [Test]
        public void Suggest_ExtraTokenAfterAllArguments_IsReadyToExecute()
        {
            // Current behavior, scheduled to change: the parser rejects the extra token, so this should be an error
            AssertHint(Suggest("wait 2 extra "), "Ready to execute");
        }

        // --- Optionals and variadic ---

        [Test]
        public void Suggest_OptionalFailsButVariadicAccepts_HintsVariadic()
        {
            AssertHint(Suggest("tag hello"), "words");
            AssertHint(Suggest("tag hello "), "words");
        }

        [Test]
        public void Suggest_AfterOptional_HintsVariadic()
        {
            AssertHint(Suggest("tag 5 "), "words");
        }

        // --- Subcommands ---

        [Test]
        public void Suggest_CommandWithSubcommands_SuggestsSubcommands()
        {
            var result = Suggest("level ");

            Assert.That(FullTexts(result), Is.EquivalentTo(new[] { "level open", "level close" }));
        }

        [Test]
        public void Suggest_PartialSubcommand_SuggestsMatchingSubcommandFirst()
        {
            var result = Suggest("level op");

            Assert.That(FullTexts(result)[0], Is.EqualTo("level open"));
            Assert.That(result[0].DisplayText, Does.Contain("subcommand"));
        }

        [Test]
        public void Suggest_AfterSubcommand_UsesSubcommandArguments()
        {
            AssertHint(Suggest("level open "), "path");
        }

        [Test]
        public void Suggest_AfterArgumentConsumed_NoLongerSuggestsSubcommands()
        {
            AssertHint(Suggest("level foo "), "Ready to execute");
        }

        // --- Limits ---

        [Test]
        public void Suggest_ManyUserValues_ReturnsAtMostFive()
        {
            // Current behavior, scheduled to change: the limit is being removed
            Assert.That(Suggest("pick ").Count, Is.EqualTo(5));
            Assert.That(Suggest("pick item").Count, Is.EqualTo(5));
        }

        private List<Suggestion> Suggest(string line)
        {
            return _engine.Suggest(line, line.Length).ToList();
        }

        private static List<string> FullTexts(List<Suggestion> suggestions)
        {
            return suggestions.Select(s => s.FullText).ToList();
        }

        private static void AssertHint(List<Suggestion> suggestions, string expectedText)
        {
            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].IsHint, Is.True);
            Assert.That(suggestions[0].DisplayText, Does.Contain(expectedText));
        }

        private static void AssertError(List<Suggestion> suggestions, string expectedText)
        {
            Assert.That(suggestions.Count, Is.EqualTo(1));
            Assert.That(suggestions[0].IsError, Is.True);
            Assert.That(suggestions[0].DisplayText, Does.Contain(expectedText));
        }
    }

    /// <summary>
    /// Suggestion functions used by the generated test commands.
    /// </summary>
    internal static class TestSuggestions
    {
        public static string[] Scenes()
        {
            return new[] { "Main Menu", "Level1", "Level2" };
        }

        public static string[] Many()
        {
            return Enumerable.Range(0, 8).Select(i => $"item{i}").ToArray();
        }
    }
}
