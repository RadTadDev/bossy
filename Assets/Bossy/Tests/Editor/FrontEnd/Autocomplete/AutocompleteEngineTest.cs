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
    /// Tests the <see cref="AutocompleteEngine"/>. The rule numbers (R1 to R31, B1 to B9, and the numbered
    /// proposals) match the comments in <see cref="CompletionWalker"/> and <see cref="AutocompleteEngine"/>, and are
    /// listed in Documentation/AutocompleteRules.md.
    /// </summary>
    internal class AutocompleteEngineTest
    {
        private AutocompleteEngine _engine;
        private Dictionary<string, string> _aliases;

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
                .WithSwitch("fast", typeof(bool), 'f')
                .Generate().GetType();

            var tag = CommandGenerator.WithName("tag")
                .WithOptional("amount", typeof(int))
                .WithVariadic("words", typeof(string)).WithSuggestions(nameof(TestSuggestions.Words), typeof(TestSuggestions))
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

            var broken = CommandGenerator.WithName("broken")
                .WithPositional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.Throws), typeof(TestSuggestions))
                .Generate().GetType();

            var fragile = CommandGenerator.WithName("fragile")
                .WithOptional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.Throws), typeof(TestSuggestions))
                .Generate().GetType();

            var counted = CommandGenerator.WithName("counted")
                .WithPositional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.Counted), typeof(TestSuggestions))
                .Generate().GetType();

            var created = CommandGenerator.WithName("created")
                .WithConstructorCall(typeof(ConstructionCounter).GetMethod(nameof(ConstructionCounter.Increment)))
                .Generate().GetType();

            var chain = CommandGenerator.WithName("chain")
                .WithSwitch("mode", typeof(string), 'm')
                .WithSwitch("verbose", typeof(bool), 'v')
                .WithPositional("kind", typeof(string))
                .WithPositional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.ThingsOfKind), typeof(TestSuggestions))
                .Generate().GetType();

            var path = CommandGenerator.WithName("path")
                .WithVariadic("parts", typeof(string)).WithSuggestions(nameof(TestSuggestions.NextPart), typeof(TestSuggestions))
                .Generate().GetType();

            var echo = CommandGenerator.WithName("echo")
                .WithPositional("text", typeof(string)).WithSuggestions(nameof(TestSuggestions.EchoCurrent), typeof(TestSuggestions))
                .Generate().GetType();

            var badName = CommandGenerator.WithName("badname")
                .WithPositional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.ReadsMissingArgument), typeof(TestSuggestions))
                .Generate().GetType();

            var badType = CommandGenerator.WithName("badtype")
                .WithPositional("count", typeof(int))
                .WithPositional("thing", typeof(string)).WithSuggestions(nameof(TestSuggestions.ReadsWrongType), typeof(TestSuggestions))
                .Generate().GetType();

            var described = CommandGenerator.WithName("described")
                .WithPositional("item", typeof(string)).WithSuggestions(nameof(TestSuggestions.Described), typeof(TestSuggestions))
                .Generate().GetType();

            var paint = CommandGenerator.WithName("paint")
                .WithPositional("level", typeof(LogType))
                .WithOptional("glossy", typeof(bool))
                .Generate().GetType();

            var script = CommandGenerator.WithName("script")
                .WithPositional("file", typeof(string)).WithValidator(typeof(Bossy.Command.EndsWithAttribute), ".cs")
                .Generate().GetType();

            var scripts = CommandGenerator.WithName("scripts")
                .WithVariadic("files", typeof(string)).WithValidator(typeof(Bossy.Command.EndsWithAttribute), ".cs")
                .Generate().GetType();

            var volume = CommandGenerator.WithName("volume")
                .WithPositional("level", typeof(int)).WithValidator(typeof(Bossy.Command.RangeAttribute), 0f, 10f)
                .Generate().GetType();

            var shaky = CommandGenerator.WithName("shaky")
                .WithPositional("thing", typeof(string)).WithValidator(typeof(ThrowingValidatorAttribute))
                .Generate().GetType();

            var types = new List<Type>
            {
                load, wait, vec, tag, pick, level, levelOpen, levelClose, broken, fragile, counted, created,
                chain, path, echo, badName, badType, described, paint, script, scripts, volume, shaky
            };
            var graph = CommandDependencyGraphBuilder.BuildGraph(types);
            var schemas = new SchemaRegistry(SchemaFactory.BuildCommandSchemas(graph));

            var adapters = new TypeAdapterRegistry();
            adapters.RegisterAdapter(new BoolAdapter());
            adapters.RegisterAdapter(new IntAdapter());
            adapters.RegisterAdapter(new FloatAdapter());
            adapters.RegisterAdapter(new StringAdapter());
            adapters.RegisterAdapter(new Vector3Adapter());
            adapters.RegisterAdapter(new EnumAdapter<LogType>());

            var context = new BossyContext(schemas, adapters, null, null, null, null);
            var operators = new OperatorList(";", "&&", "||", "|", "!");

            _aliases = new Dictionary<string, string>();
            _engine = new AutocompleteEngine(context, operators, _aliases);
        }

        [SetUp]
        public void ResetAliases()
        {
            _aliases.Clear();
        }

        // --- Input and cursor (R1 to R3, proposal 6) ---

        [Test]
        public void Complete_EmptyInput_ReturnsNothing()
        {
            Assert.That(Complete("").IsEmpty, Is.True);
            Assert.That(Complete("   ").IsEmpty, Is.True);
        }

        [Test]
        public void Complete_CursorInsideToken_ReplacesWholeTokenAndKeepsTextAfterIt()
        {
            var line = "load Lev extra";
            var result = _engine.Complete(line, 8);

            Assert.That(result.Completions[0].ApplyTo(line), Is.EqualTo("load Level1 extra"));
            Assert.That(result.Completions[0].CursorAfterApply, Is.EqualTo("load Level1".Length));
        }

        [Test]
        public void Complete_CursorRightBeforeToken_InsertsWithSeparatingSpace()
        {
            var line = "load -f";
            var result = _engine.Complete(line, 5);

            Assert.That(Applied(line, result), Does.Contain("load Level1 -f"));
        }

        // --- Command names (R4 to R6, proposals 7 and 8, B4) ---

        [Test]
        public void Complete_TypingCommandName_SuggestsMatchingCommands()
        {
            var result = Complete("lo");

            Assert.That(Applied("lo", result)[0], Is.EqualTo("load"));
            Assert.That(result.Completions[0].Kind, Is.EqualTo(CompletionKind.Command));
        }

        [Test]
        public void Complete_NoCommandMatchesWhileTyping_Hints()
        {
            AssertHint(Complete("xyzq"), "No command matches 'xyzq'");
        }

        [Test]
        public void Complete_NoCommandMatchesMovedOn_ReturnsError()
        {
            AssertError(Complete("xyzq "), "No command with name \"xyzq\"");
            AssertError(Complete("xyzq -f "), "No command with name \"xyzq\"");
        }

        [Test]
        public void Complete_MisspelledCommand_ResolvesToBestMatchAndOffersFixFirst()
        {
            var line = "laod ";
            var result = Complete(line);

            Assert.That(result.Completions[0].Kind, Is.EqualTo(CompletionKind.Command));
            Assert.That(result.Completions[0].ApplyTo(line), Is.EqualTo("load "));
            Assert.That(Applied(line, result).Skip(1), Is.EqualTo(new[] { "laod \"Main Menu\"", "laod Level1", "laod Level2" }));
        }

        [Test]
        public void Complete_TypedText_IsReportedAsMatchRanges()
        {
            var result = Complete("load Lev");

            var level = result.Completions.First(c => c.DisplayText == "Level1");
            Assert.That(level.MatchRanges, Is.EqualTo(new[] { new MatchRange(0, 3) }));
        }

        [Test]
        public void Complete_MisspelledCommandFix_HasNoMatchRanges()
        {
            var result = Complete("laod Lev");

            Assert.That(result.Completions[0].Kind, Is.EqualTo(CompletionKind.Command));
            Assert.That(result.Completions[0].MatchRanges, Is.Empty);
            Assert.That(result.Completions[1].MatchRanges, Is.Not.Empty);
        }

        [Test]
        public void Complete_AliasName_IsSuggestedWithItsExpansion()
        {
            _aliases["ld"] = "load Level1";

            var result = Complete("ld");

            var alias = result.Completions.First(c => c.Kind == CompletionKind.Alias);
            Assert.That(alias.DisplayText, Is.EqualTo("ld"));
            Assert.That(alias.Description, Is.EqualTo("load Level1"));
        }

        [Test]
        public void Complete_AfterAlias_CompletesTheExpandedCommand()
        {
            _aliases["ld"] = "load";

            Assert.That(Applied("ld ", Complete("ld ")), Does.Contain("ld Level1"));
        }

        // --- Operators (R7, R8, proposals 1 and 2, B1, B9) ---

        [Test]
        public void Complete_AfterOperator_SuggestsNextCommandKeepingPrefix()
        {
            Assert.That(Applied("wait 2 && lo", Complete("wait 2 && lo"))[0], Is.EqualTo("wait 2 && load"));
        }

        [Test]
        public void Complete_AfterOperatorWithSpace_ListsAllCommands()
        {
            Assert.That(Applied("wait 2 && ", Complete("wait 2 && ")), Does.Contain("wait 2 && load"));
        }

        [Test]
        public void Complete_OperatorAfterInvalidValue_ReturnsParserError()
        {
            AssertError(Complete("wait abc && lo"), "Could not parse \"abc\"");
        }

        [Test]
        public void Complete_OperatorAfterIncompleteCommand_ReturnsParserError()
        {
            AssertError(Complete("wait && lo"), "Expected argument \"seconds\"");
        }

        [Test]
        public void Complete_OperatorAtStart_ReturnsError()
        {
            AssertError(Complete("&& lo"), "&&");
        }

        [Test]
        public void Complete_CommandsBeforeOperator_AreCheckedWithoutBeingCreated()
        {
            ConstructionCounter.Count = 0;

            Assert.That(Applied("created && lo", Complete("created && lo"))[0], Is.EqualTo("created && load"));
            AssertReady(Complete("created !"), "in window");

            Assert.That(ConstructionCounter.Count, Is.Zero);
        }

        [Test]
        public void Complete_TrailingWindowOperator_IsReadyToExecuteInWindow()
        {
            AssertReady(Complete("wait 2 !"), "in window");
        }

        [Test]
        public void Complete_WindowOperatorAfterIncompleteCommand_ReturnsParserError()
        {
            AssertError(Complete("wait !"), "Expected argument \"seconds\"");
        }

        [Test]
        public void Complete_WindowOperatorInMiddle_ReturnsParserError()
        {
            AssertError(Complete("wait 2 ! lo"), "Window operator may only appear first or last");
        }

        [Test]
        public void Complete_LeadingWindowOperator_IsSkipped()
        {
            Assert.That(Applied("!wa", Complete("!wa"))[0], Is.EqualTo("!wait"));
            AssertReady(Complete("!wait 2 "), "Ready");
        }

        [Test]
        public void Complete_QuotedWindowOperator_IsPlainValue()
        {
            AssertReady(Complete("load \"!\" "), "Ready");
        }

        // --- Quotes (proposal 5, B2) ---

        [Test]
        public void Complete_UnfinishedQuote_CompletesInsideQuoteAndClosesIt()
        {
            var line = "load \"Main";
            var result = Complete(line);

            Assert.That(result.Message, Does.Contain("scene"));
            Assert.That(Applied(line, result)[0], Is.EqualTo("load \"Main Menu\""));
        }

        [Test]
        public void Complete_UnfinishedQuoteWithSpace_StaysInsideQuote()
        {
            var line = "load \"Main M";

            Assert.That(Applied(line, Complete(line))[0], Is.EqualTo("load \"Main Menu\""));
        }

        [Test]
        public void Complete_UnfinishedQuoteWithNoArgumentLeft_ReturnsError()
        {
            AssertError(Complete("level close \"x"), "unexpected token");
        }

        [Test]
        public void Complete_AfterQuotedValue_KeepsQuotes()
        {
            var line = "load \"Main Menu\" -";

            Assert.That(Applied(line, Complete(line)), Is.EquivalentTo(new[]
            {
                "load \"Main Menu\" --force",
                "load \"Main Menu\" --verbose",
                "load \"Main Menu\" --count"
            }));
        }

        // --- Long switches (R10, R11, R15, B8) ---

        [Test]
        public void Complete_DoubleDash_SuggestsAllLongSwitches()
        {
            var result = Complete("load --");

            Assert.That(Applied("load --", result), Is.EquivalentTo(new[] { "load --force", "load --verbose", "load --count" }));
            Assert.That(result.Completions.Select(c => c.DisplayText), Does.Contain("-c, --count <int>"));
            Assert.That(result.Completions.Select(c => c.DisplayText), Does.Contain("-f, --force"));
        }

        [Test]
        public void Complete_PartialLongSwitch_SuggestsMatchingSwitchFirst()
        {
            Assert.That(Applied("load --ver", Complete("load --ver"))[0], Is.EqualTo("load --verbose"));
        }

        [Test]
        public void Complete_DashTokenAfterSwitchSurrender_IsValue()
        {
            AssertReady(Complete("load -- -x "), "Ready");
        }

        [Test]
        public void Complete_UnknownSwitch_ReturnsParserError()
        {
            AssertError(Complete("load --nope "), "Unknown switch \"--nope\"");
        }

        // --- Short switches (R12 to R14) ---

        [Test]
        public void Complete_SingleDash_SuggestsLongSwitches()
        {
            Assert.That(Applied("load -", Complete("load -")), Is.EquivalentTo(new[] { "load --force", "load --verbose", "load --count" }));
        }

        [Test]
        public void Complete_SingleDashMovedOn_ReturnsError()
        {
            AssertError(Complete("load - "), "'-' is not a valid token");
        }

        [Test]
        public void Complete_ShortSwitch_SuggestsShortNamesWithMatchFirst()
        {
            var result = Complete("load -v");

            Assert.That(result.Completions.Count, Is.EqualTo(3));
            Assert.That(Applied("load -v", result)[0], Is.EqualTo("load -v"));
        }

        [Test]
        public void Complete_AggregateBoolSwitches_Hints()
        {
            AssertHint(Complete("load -fv"), "Aggregate flags");
        }

        [Test]
        public void Complete_AggregateBoolSwitchesMovedOn_ContinuesToNextArgument()
        {
            Assert.That(Applied("load -fv ", Complete("load -fv ")), Does.Contain("load -fv Level1"));
        }

        [Test]
        public void Complete_AggregateWithNonBoolSwitch_ReturnsParserError()
        {
            AssertError(Complete("load -fc"), "takes a value");
        }

        [Test]
        public void Complete_SingleDashLongSwitchPrefix_SuggestsLongSwitch()
        {
            Assert.That(Applied("load -verb", Complete("load -verb")), Is.EqualTo(new[] { "load --verbose" }));
        }

        [Test]
        public void Complete_NegativeNumber_IsValueNotSwitch()
        {
            AssertReady(Complete("wait -5 "), "Ready");
        }

        // --- Switch values (R16 to R18) ---

        [Test]
        public void Complete_AfterBoolSwitch_ContinuesToNextArgument()
        {
            Assert.That(Applied("load --force ", Complete("load --force ")), Does.Contain("load --force Level1"));
        }

        [Test]
        public void Complete_AfterValueSwitch_HintsItsType()
        {
            AssertHint(Complete("load --count "), "count <int>");
        }

        [Test]
        public void Complete_InvalidSwitchValueMovedOn_ReturnsParserError()
        {
            AssertError(Complete("load --count abc "), "Could not parse \"abc\"");
        }

        [Test]
        public void Complete_InvalidSwitchValueAtCursor_HintsInvalidValue()
        {
            AssertInvalid(Complete("load --count abc"), "count", "abc");
        }

        [Test]
        public void Complete_ValidSwitchValueMovedOn_ContinuesToNextArgument()
        {
            Assert.That(Applied("load --count 5 ", Complete("load --count 5 ")), Does.Contain("load --count 5 Level1"));
        }

        // --- Ordered values (R20 to R23, proposals 4, 10 and 13) ---

        [Test]
        public void Complete_NextPositional_HintsAndSuggestsUserValuesInOrderWithQuotes()
        {
            var result = Complete("load ");

            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Hint));
            Assert.That(result.Message, Does.Contain("scene <string>"));
            Assert.That(Applied("load ", result), Is.EqualTo(new[] { "load \"Main Menu\"", "load Level1", "load Level2" }));
            Assert.That(result.Completions[1].DisplayText, Is.EqualTo("Level1"));
            Assert.That(result.Completions[1].Kind, Is.EqualTo(CompletionKind.Value));
        }

        [Test]
        public void Complete_PartialPositional_RanksPrefixMatchesFirst()
        {
            var result = Applied("load Lev", Complete("load Lev"));

            Assert.That(result[0], Is.EqualTo("load Level1"));
            Assert.That(result[1], Is.EqualTo("load Level2"));
        }

        [Test]
        public void Complete_NextPositionalWithoutSuggestions_HintsItsType()
        {
            var result = Complete("wait ");

            AssertHint(result, "seconds <float>");
            Assert.That(result.Completions, Is.Empty);
        }

        [Test]
        public void Complete_InvalidPositionalMovedOn_ReturnsParserError()
        {
            AssertError(Complete("wait abc "), "Could not parse \"abc\" as type \"float\"");
        }

        [Test]
        public void Complete_InvalidPositionalAtCursor_HintsInvalidValue()
        {
            AssertInvalid(Complete("wait abc"), "seconds", "abc");
        }

        [Test]
        public void Complete_MultiTokenValueWithTooFewTokens_HintsProgressWithoutError()
        {
            var result = Complete("vec 1 2");

            AssertHint(result, "pos <Vector3> 2/3");
            Assert.That(result.Argument.TokenNumber, Is.EqualTo(2));
            Assert.That(result.Argument.TokensDesired, Is.EqualTo(3));
            Assert.That(result.Argument.InvalidValue, Is.Null);
            AssertHint(Complete("vec 1 2 "), "pos <Vector3> 3/3");
            AssertHint(Complete("vec "), "pos <Vector3> 1/3");
        }

        [Test]
        public void Complete_ArgumentHint_IsShortAndCarriesTheArgument()
        {
            var result = Complete("load ");

            Assert.That(result.Message, Is.EqualTo("scene <string>"));
            Assert.That(result.Argument.Argument.Name, Is.EqualTo("scene"));
            Assert.That(result.Argument.HasProgress, Is.False);
        }

        [Test]
        public void Complete_MultiTokenValueInvalid_HintsInvalidValue()
        {
            AssertInvalid(Complete("vec 1 2 x"), "pos", "1 2 x");
        }

        [Test]
        public void Complete_SwitchInsideMultiTokenValue_ReturnsParserError()
        {
            AssertError(Complete("vec 1 -f 2 3"), "\"-f\" splits the value of argument \"pos\"");
            AssertError(Complete("vec 1 -- 2 3"), "\"--\" splits the value of argument \"pos\"");
        }

        [Test]
        public void Complete_SwitchAfterMultiTokenValue_IsAllowed()
        {
            AssertReady(Complete("vec -f 1 2 3 "), "Ready");
            AssertReady(Complete("vec 1 2 3 -f "), "Ready");
        }

        [Test]
        public void Complete_AllArgumentsFilled_IsReadyToExecute()
        {
            AssertReady(Complete("wait 2 "), "Ready");
        }

        [Test]
        public void Complete_ReadyWithSwitchesLeft_ListsThem()
        {
            var result = Complete("load Level1 --force ");

            AssertReady(result, "Ready  2 switches");
            Assert.That(Applied("load Level1 --force ", result), Is.EquivalentTo(new[]
            {
                "load Level1 --force --verbose",
                "load Level1 --force --count"
            }));
        }

        [Test]
        public void Complete_ExtraTokenAfterAllArguments_ReturnsParserError()
        {
            AssertError(Complete("wait 2 extra "), "unexpected token(s): \"extra\"");
        }

        // --- Optionals and variadic (R22, R24, B5, B6) ---

        [Test]
        public void Complete_OptionalFailsButVariadicAccepts_HintsVariadic()
        {
            AssertHint(Complete("tag hello"), "words");
            AssertReady(Complete("tag hello "), "1 optional");
        }

        [Test]
        public void Complete_AfterOptional_OffersVariadicValues()
        {
            var result = Complete("tag 5 ");

            AssertReady(result, "1 optional");
            Assert.That(Applied("tag 5 ", result), Does.Contain("tag 5 \"two words\""));
        }

        [Test]
        public void Complete_VariadicValues_QuoteWhitespaceAndSwitchLikeText()
        {
            var result = Applied("tag 5 x ", Complete("tag 5 x "));

            Assert.That(result, Does.Contain("tag 5 x \"two words\""));
            Assert.That(result, Does.Contain("tag 5 x \"-dash\""));
            Assert.That(result, Does.Contain("tag 5 x \"a&&b\""));
        }

        // --- Subcommands (R19, R25, R26) ---

        [Test]
        public void Complete_CommandWithSubcommands_SuggestsSubcommands()
        {
            var result = Complete("level ");

            Assert.That(Applied("level ", result), Is.EquivalentTo(new[] { "level open", "level close" }));
        }

        [Test]
        public void Complete_PartialSubcommand_SuggestsMatchingSubcommandFirst()
        {
            var result = Complete("level op");

            Assert.That(Applied("level op", result)[0], Is.EqualTo("level open"));
            Assert.That(result.Completions[0].Kind, Is.EqualTo(CompletionKind.Subcommand));
        }

        [Test]
        public void Complete_AfterSubcommand_UsesSubcommandArguments()
        {
            AssertHint(Complete("level open "), "path");
        }

        [Test]
        public void Complete_AfterArgumentConsumed_NoLongerSuggestsSubcommands()
        {
            var result = Complete("level foo ");

            AssertReady(result, "Ready");
            Assert.That(result.Completions, Is.Empty);
        }

        // --- Suggestion functions (proposal 14, caching) ---

        [Test]
        public void Complete_ManyUserValues_AreNotCapped()
        {
            Assert.That(Complete("pick ").Completions.Count, Is.EqualTo(8));
            Assert.That(Complete("pick item").Completions.Count, Is.EqualTo(8));
        }

        [Test]
        public void Complete_SuggestionFunctionThrows_ReportsItApartFromHint()
        {
            var result = Complete("broken ");

            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Hint));
            Assert.That(result.Message, Is.EqualTo("thing <string>"));
            Assert.That(result.SuggestionFailure.Argument.Name, Is.EqualTo("thing"));
            Assert.That(result.SuggestionFailure.Message, Is.EqualTo("Broken on purpose"));
        }

        [Test]
        public void Complete_SuggestionFunctionThrowsWhenReady_ReportsItApartFromReady()
        {
            var result = Complete("fragile ");

            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Ready));
            Assert.That(result.Message, Is.EqualTo("Ready  1 optional"));
            Assert.That(result.SuggestionFailure.Message, Is.EqualTo("Broken on purpose"));
        }

        [Test]
        public void Complete_SuggestionFunctionWorks_HasNoFailure()
        {
            Assert.That(Complete("load ").SuggestionFailure, Is.Null);
        }

        [Test]
        public void Complete_TypingInSameSlot_RunsSuggestionFunctionOnce()
        {
            TestSuggestions.CountedCalls = 0;

            Complete("counted ");
            Complete("counted a");
            Complete("counted al");

            Assert.That(TestSuggestions.CountedCalls, Is.EqualTo(1));
        }

        [Test]
        public void Complete_LeavingSlotAndComingBack_RunsSuggestionFunctionAgain()
        {
            TestSuggestions.CountedCalls = 0;

            Complete("counted a");
            Complete("counte");
            Complete("counted a");

            Assert.That(TestSuggestions.CountedCalls, Is.EqualTo(2));
        }

        // --- Suggestion API: typed reads, tracked caching, rich values ---

        [Test]
        public void Complete_SuggestionFunction_ReadsEarlierPositionalWithTryGet()
        {
            Assert.That(Complete("chain fruit ").Completions.Select(c => c.DisplayText), Does.Contain("fruit-1"));
        }

        [Test]
        public void Complete_SuggestionFunction_ReadsSwitchTypedAnywhereBefore()
        {
            Assert.That(Complete("chain -m fast fruit ").Completions.Select(c => c.DisplayText), Does.Contain("mode-fast"));
        }

        [Test]
        public void Complete_TryGetOnArgumentNotTyped_ReturnsFalse()
        {
            Assert.That(Complete("chain fruit ").Completions.Select(c => c.DisplayText), Does.Contain("no mode"));
        }

        [Test]
        public void Complete_ValueReadByFunctionChanges_RunsFunctionAgain()
        {
            TestSuggestions.ThingsOfKindCalls = 0;

            Complete("chain fruit ");
            Complete("chain fruit a");
            Complete("chain color ");

            Assert.That(TestSuggestions.ThingsOfKindCalls, Is.EqualTo(2));
            Assert.That(Complete("chain color ").Completions.Select(c => c.DisplayText), Does.Contain("color-1"));
        }

        [Test]
        public void Complete_TextNotReadByFunctionChanges_KeepsCachedValues()
        {
            TestSuggestions.ThingsOfKindCalls = 0;

            Complete("chain fruit ");
            Complete("chain -v fruit ");

            Assert.That(TestSuggestions.ThingsOfKindCalls, Is.EqualTo(1));
        }

        [Test]
        public void Complete_FunctionReadingCurrent_RunsOnEveryKeystroke()
        {
            TestSuggestions.EchoCalls = 0;

            Complete("echo a");
            var result = Complete("echo ab");

            Assert.That(TestSuggestions.EchoCalls, Is.EqualTo(2));
            Assert.That(result.Completions.Select(c => c.DisplayText), Does.Contain("ab!"));
        }

        [Test]
        public void Complete_VariadicTryGet_ReturnsElementsTypedBefore()
        {
            Assert.That(Complete("path ").Completions.Select(c => c.DisplayText), Does.Contain("first"));
            Assert.That(Complete("path a b ").Completions.Select(c => c.DisplayText), Does.Contain("after a/b"));
        }

        [Test]
        public void Complete_TryGetUnknownArgument_ReportsFailure()
        {
            var result = Complete("badname ");

            Assert.That(result.SuggestionFailure, Is.Not.Null);
            Assert.That(result.SuggestionFailure.Message, Does.Contain("no argument named 'missing'"));
        }

        [Test]
        public void Complete_TryGetWithWrongType_ReportsFailure()
        {
            var result = Complete("badtype 5 ");

            Assert.That(result.SuggestionFailure, Is.Not.Null);
            Assert.That(result.SuggestionFailure.Message, Does.Contain("not a string"));
        }

        [Test]
        public void Complete_RichSuggestion_ShowsItsDescription()
        {
            var completion = Complete("described ").Completions.Single(c => c.DisplayText == "sword");

            Assert.That(completion.Description, Is.EqualTo("A sharp blade"));
        }

        [Test]
        public void Complete_EnumArgument_SuggestsItsNames()
        {
            var shown = Complete("paint ").Completions.Select(c => c.DisplayText);

            Assert.That(shown, Is.EquivalentTo(Enum.GetNames(typeof(LogType))));
        }

        [Test]
        public void Complete_BoolArgument_SuggestsTrueAndFalse()
        {
            var result = Complete("paint Error ");

            AssertReady(result, "Ready");
            Assert.That(result.Completions.Select(c => c.DisplayText), Is.EquivalentTo(new[] { "true", "false" }));
        }

        // --- Validators ---

        [Test]
        public void Complete_ValueBeforeCursorFailsValidator_IsError()
        {
            var result = Complete("script foo.txt ");

            AssertError(result, "(Argument 'file')");
            Assert.That(result.Message, Does.Contain("must end with '.cs'"));
        }

        [Test]
        public void Complete_ValueBeforeCursorPassesValidator_IsReady()
        {
            AssertReady(Complete("script Foo.cs "), "Ready");
        }

        [Test]
        public void Complete_ValueAtCursorFailsValidator_MarksConstraintWithoutError()
        {
            var result = Complete("script Fo");

            AssertHint(result, "file <string>  fails: ends with .cs");
            Assert.That(result.Argument.InvalidValue, Is.Null);
            Assert.That(result.Argument.ValidationError, Does.Contain("must end with '.cs'"));
            Assert.That(result.Argument.FailedConstraint, Is.EqualTo(0));
        }

        [Test]
        public void Complete_ValueAtCursorPassesValidator_HasNoValidationError()
        {
            var result = Complete("script Foo.cs");

            Assert.That(result.Message, Is.EqualTo("file <string>"));
            Assert.That(result.Argument.ValidationError, Is.Null);
            Assert.That(result.Argument.FailedConstraint, Is.EqualTo(-1));
        }

        [Test]
        public void Complete_PartialNumberOutOfRange_MarksConstraint()
        {
            Assert.That(Complete("volume 1").Argument.ValidationError, Is.Null);
            Assert.That(Complete("volume 15").Argument.FailedConstraint, Is.EqualTo(0));
            AssertError(Complete("volume 15 "), "(Argument 'level')");
        }

        [Test]
        public void Complete_VariadicElements_AreValidatedOneByOne()
        {
            AssertReady(Complete("scripts a.cs b.cs "), "Ready");
            AssertError(Complete("scripts a.cs b.txt "), "(Argument 'files')");
            Assert.That(Complete("scripts a.cs b").Argument.FailedConstraint, Is.EqualTo(0));
            Assert.That(Complete("scripts a.cs b.cs").Argument.ValidationError, Is.Null);
        }

        [Test]
        public void Complete_ValidatorThrows_ReportsFailureAndTreatsValueAsValid()
        {
            var atCursor = Complete("shaky x");

            AssertHint(atCursor, "thing <string>");
            Assert.That(atCursor.Argument.ValidationError, Is.Null);
            Assert.That(atCursor.SuggestionFailure.IsValidator, Is.True);
            Assert.That(atCursor.SuggestionFailure.Message, Is.EqualTo(ThrowingValidatorAttribute.Message));

            var moved = Complete("shaky x ");

            AssertReady(moved, "Ready");
            Assert.That(moved.SuggestionFailure.IsValidator, Is.True);
        }

        private CompletionResult Complete(string line)
        {
            return _engine.Complete(line, line.Length);
        }

        private static List<string> Applied(string line, CompletionResult result)
        {
            return result.Completions.Select(c => c.ApplyTo(line)).ToList();
        }

        private static void AssertHint(CompletionResult result, string expectedText)
        {
            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Hint));
            Assert.That(result.Message, Does.Contain(expectedText));
        }

        private static void AssertInvalid(CompletionResult result, string argumentName, string invalidValue)
        {
            AssertHint(result, "  invalid");
            Assert.That(result.Argument.Argument.Name, Is.EqualTo(argumentName));
            Assert.That(result.Argument.InvalidValue, Is.EqualTo(invalidValue));
        }

        private static void AssertReady(CompletionResult result, string expectedText)
        {
            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Ready));
            Assert.That(result.Message, Does.Contain(expectedText));
        }

        private static void AssertError(CompletionResult result, string expectedText)
        {
            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Error));
            Assert.That(result.Message, Does.Contain(expectedText).IgnoreCase);
            Assert.That(result.Completions, Is.Empty);
        }
    }

    /// <summary>
    /// Suggestion functions used by the generated test commands.
    /// </summary>
    internal static class TestSuggestions
    {
        public static int CountedCalls;
        public static int ThingsOfKindCalls;
        public static int EchoCalls;

        public static string[] Scenes()
        {
            return new[] { "Main Menu", "Level1", "Level2" };
        }

        public static string[] Many()
        {
            return Enumerable.Range(0, 8).Select(i => $"item{i}").ToArray();
        }

        public static string[] Words()
        {
            return new[] { "two words", "-dash", "a&&b" };
        }

        public static string[] Throws()
        {
            throw new InvalidOperationException("Broken on purpose");
        }

        public static string[] Counted()
        {
            CountedCalls++;
            return new[] { "alpha", "beta" };
        }

        public static string[] ThingsOfKind(SuggestionContext ctx)
        {
            ThingsOfKindCalls++;
            ctx.TryGet<string>("kind", out var kind);
            var mode = ctx.TryGet<string>("mode", out var m) ? $"mode-{m}" : "no mode";
            return new[] { $"{kind}-1", $"{kind}-2", mode };
        }

        public static string[] NextPart(SuggestionContext ctx)
        {
            return ctx.TryGet<string[]>("parts", out var parts)
                ? new[] { $"after {string.Join("/", parts)}" }
                : new[] { "first" };
        }

        public static string[] EchoCurrent(SuggestionContext ctx)
        {
            EchoCalls++;
            return new[] { $"{ctx.Current}!" };
        }

        public static string[] ReadsMissingArgument(SuggestionContext ctx)
        {
            ctx.TryGet<string>("missing", out _);
            return Array.Empty<string>();
        }

        public static string[] ReadsWrongType(SuggestionContext ctx)
        {
            ctx.TryGet<string>("count", out _);
            return Array.Empty<string>();
        }

        public static IEnumerable<Suggestion> Described()
        {
            yield return new Suggestion("sword", "A sharp blade");
            yield return "shield";
        }
    }
}
