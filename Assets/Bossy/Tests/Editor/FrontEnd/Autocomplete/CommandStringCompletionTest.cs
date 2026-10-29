using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Autocomplete
{
    /// <summary>
    /// Tests how the <see cref="AutocompleteEngine"/> completes <see cref="CommandString"/> arguments: command names
    /// when unquoted, and whole commands inside an open quote.
    /// </summary>
    internal class CommandStringCompletionTest
    {
        private AutocompleteEngine _engine;
        private Dictionary<string, string> _aliases;

        [OneTimeSetUp]
        public void Setup()
        {
            var watch = CommandGenerator.WithName("watch")
                .WithSwitch("interval", typeof(float), 'i')
                .WithPositional("command", typeof(CommandString))
                .Generate().GetType();

            var get = CommandGenerator.WithName("get")
                .WithSwitch("name", typeof(string), 'n')
                .WithPositional("target", typeof(string)).WithSuggestions(nameof(CommandStringSuggestions.Targets), typeof(CommandStringSuggestions))
                .Generate().GetType();

            var ping = CommandGenerator.WithName("ping")
                .Generate().GetType();

            var each = CommandGenerator.WithName("each")
                .WithVariadic("commands", typeof(CommandString))
                .Generate().GetType();

            var types = new List<Type> { watch, get, ping, each };
            var graph = CommandDependencyGraphBuilder.BuildGraph(types);
            var schemas = new SchemaRegistry(SchemaFactory.BuildCommandSchemas(graph));

            var adapters = new TypeAdapterRegistry();
            adapters.RegisterAdapter(new FloatAdapter());
            adapters.RegisterAdapter(new StringAdapter());
            adapters.RegisterAdapter(new CommandStringAdapter());

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

        // --- Unquoted ---

        [Test]
        public void Complete_EmptyCommandString_SuggestsCommandNamesAndAliases()
        {
            _aliases["health"] = "get -n hp Player";

            var result = Complete("watch ");

            Assert.That(Applied("watch ", result), Is.SupersetOf(new[] { "watch get", "watch ping", "watch watch", "watch health" }));
            Assert.That(result.Completions.First(c => c.DisplayText == "health").Kind, Is.EqualTo(CompletionKind.Alias));
        }

        [Test]
        public void Complete_TypingUnquotedCommandString_SuggestsMatchingCommandNames()
        {
            var result = Complete("watch pi");

            Assert.That(Applied("watch pi", result)[0], Is.EqualTo("watch ping"));
            Assert.That(result.Completions[0].Kind, Is.EqualTo(CompletionKind.Command));
        }

        [Test]
        public void Complete_UnquotedCommandThatNeedsArguments_IsMarkedInvalidAtCursor()
        {
            var result = Complete("watch get");

            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Hint));
            Assert.That(result.Argument.ValidationError, Does.Contain("Quote the command to give it arguments."));
        }

        [Test]
        public void Complete_UnquotedCommandThatNeedsArguments_IsErrorOnceMovedOn()
        {
            AssertError(Complete("watch get "), "Invalid command");
        }

        [Test]
        public void Complete_UnquotedCommandWithoutArguments_IsReady()
        {
            var result = Complete("watch ping ");

            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Ready));
        }

        [Test]
        public void Complete_AliasAsCommandString_IsOneValue()
        {
            _aliases["health"] = "get -n hp Player";

            Assert.That(Complete("watch health ").MessageKind, Is.EqualTo(CompletionMessageKind.Ready));
        }

        [Test]
        public void Complete_CommandStringVariadic_SuggestsCommandNamesForEachElement()
        {
            Assert.That(Applied("each ping pi", Complete("each ping pi"))[0], Is.EqualTo("each ping ping"));
        }

        // --- Inside an open quote ---

        [Test]
        public void Complete_JustOpenedQuote_SuggestsCommandNames()
        {
            Assert.That(Applied("watch \"", Complete("watch \"")), Does.Contain("watch \"ping"));
        }

        [Test]
        public void Complete_CommandNameInsideQuote_IsCompletedInsideIt()
        {
            var result = Complete("watch \"pi");

            Assert.That(Applied("watch \"pi", result)[0], Is.EqualTo("watch \"ping"));
            Assert.That(result.Completions[0].MatchRanges, Is.EqualTo(new[] { new MatchRange(0, 2) }));
        }

        [Test]
        public void Complete_SwitchInsideQuote_ListsTheInnerCommandsSwitches()
        {
            Assert.That(Applied("watch \"get -", Complete("watch \"get -")), Does.Contain("watch \"get --name"));
        }

        [Test]
        public void Complete_ValueInsideQuote_UsesTheOtherQuote()
        {
            Assert.That(Applied("watch \"get ", Complete("watch \"get ")), Does.Contain("watch \"get 'Main Camera'"));
        }

        [Test]
        public void Complete_InsideSingleQuote_ValuesUseDoubleQuotes()
        {
            Assert.That(Applied("watch 'get ", Complete("watch 'get ")), Does.Contain("watch 'get \"Main Camera\""));
        }

        [Test]
        public void Complete_CompleteInnerCommand_IsReady()
        {
            Assert.That(Complete("watch \"get Player ").MessageKind, Is.EqualTo(CompletionMessageKind.Ready));
        }

        [Test]
        public void Complete_InnerCommandError_IsShown()
        {
            AssertError(Complete("watch \"xyzq "), "No command with name \"xyzq\"");
        }

        [Test]
        public void Complete_CursorInsideClosedQuote_ReplacesOnlyTheInnerToken()
        {
            var line = "watch \"ge -n x\"";
            var result = _engine.Complete(line, "watch \"ge".Length);

            Assert.That(result.Completions[0].ApplyTo(line), Is.EqualTo("watch \"get -n x\""));
        }

        [Test]
        public void Complete_NestedQuotes_CompleteAtEveryLevel()
        {
            Assert.That(Applied("watch \"watch 'pi", Complete("watch \"watch 'pi"))[0], Is.EqualTo("watch \"watch 'ping"));
        }

        [Test]
        public void Complete_InnerOperators_CompleteTheNextInnerCommand()
        {
            Assert.That(Applied("watch \"ping && pi", Complete("watch \"ping && pi"))[0], Is.EqualTo("watch \"ping && ping"));
        }

        [Test]
        public void Complete_QuotedVariadicElement_IsCompletedInsideIt()
        {
            Assert.That(Applied("each ping \"get -", Complete("each ping \"get -")), Does.Contain("each ping \"get --name"));
        }

        private CompletionResult Complete(string line)
        {
            return _engine.Complete(line, line.Length);
        }

        private static List<string> Applied(string line, CompletionResult result)
        {
            return result.Completions.Select(c => c.ApplyTo(line)).ToList();
        }

        private static void AssertError(CompletionResult result, string expectedText)
        {
            Assert.That(result.MessageKind, Is.EqualTo(CompletionMessageKind.Error));
            Assert.That(result.Message, Does.Contain(expectedText).IgnoreCase);
        }
    }

    /// <summary>
    /// Suggestion functions used by the generated command string test commands.
    /// </summary>
    internal static class CommandStringSuggestions
    {
        public static string[] Targets()
        {
            return new[] { "Player", "Main Camera" };
        }
    }
}
