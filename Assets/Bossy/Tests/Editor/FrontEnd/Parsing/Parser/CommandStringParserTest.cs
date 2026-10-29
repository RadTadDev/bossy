using System;
using System.Collections.Generic;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests how the <see cref="Parser"/> reads <see cref="CommandString"/> arguments and expands aliases.
    /// </summary>
    internal class CommandStringParserTest
    {
        private readonly OperatorList _ops = new(";", "&&", "||", "|", "!");
        private Parser _parser;
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
                .WithPositional("target", typeof(string))
                .Generate().GetType();

            var ping = CommandGenerator.WithName("ping")
                .Generate().GetType();

            var each = CommandGenerator.WithName("each")
                .WithVariadic("commands", typeof(CommandString))
                .Generate().GetType();

            var then = CommandGenerator.WithName("then")
                .WithSwitch("command", typeof(CommandString), 'c')
                .Generate().GetType();

            var pair = CommandGenerator.WithName("pair")
                .WithPositional("first", typeof(int))
                .WithPositional("second", typeof(int))
                .Generate().GetType();

            var created = CommandGenerator.WithName("created")
                .WithConstructorCall(typeof(ConstructionCounter).GetMethod(nameof(ConstructionCounter.Increment)))
                .Generate().GetType();

            var types = new List<Type> { watch, get, ping, each, then, pair, created };
            var graph = CommandDependencyGraphBuilder.BuildGraph(types);
            var schemas = new SchemaRegistry(SchemaFactory.BuildCommandSchemas(graph));

            var adapters = new TypeAdapterRegistry();
            adapters.RegisterAdapter(new IntAdapter());
            adapters.RegisterAdapter(new FloatAdapter());
            adapters.RegisterAdapter(new StringAdapter());
            adapters.RegisterAdapter(new CommandStringAdapter());

            _parser = new Parser(schemas, adapters);
            _aliases = new Dictionary<string, string>();
        }

        [SetUp]
        public void ResetAliases()
        {
            _aliases.Clear();
        }

        // --- Reading command strings ---

        [Test]
        public void Parse_UnquotedCommandWithoutArguments_IsTheValue()
        {
            Assert.That(ParseValue("watch ping", "command"), Is.EqualTo(new CommandString("ping")));
        }

        [Test]
        public void Parse_QuotedCommand_KeepsItsSwitchesAndArguments()
        {
            Assert.That(ParseValue("watch -i 2 \"get -n hp Player\"", "command"), Is.EqualTo(new CommandString("get -n hp Player")));
        }

        [Test]
        public void Parse_QuotedCommandWithOperators_IsOneValue()
        {
            var result = _parser.Parse("watch \"get x | ping && ping\"", _ops);

            Assert.That(result.TryGetGraph(out var graph), Is.True, result.Message);
            Assert.That(graph.ToArray().Length, Is.EqualTo(1));
        }

        [Test]
        public void Parse_NestedCommandStrings_AreCheckedAtEveryLevel()
        {
            AssertParses("watch \"watch 'get Player'\"");
            AssertError("watch \"watch 'get'\"", "Invalid command");
        }

        [Test]
        public void Parse_UnquotedCommandThatNeedsArguments_IsInvalid()
        {
            var result = _parser.Parse("watch get", _ops);

            Assert.That(result.TryGetGraph(out _), Is.False);
            Assert.That(result.Message, Does.Contain("(Argument 'command') Invalid command"));
            Assert.That(result.Message, Does.Contain("Quote the command to give it arguments."));
        }

        [Test]
        public void Parse_UnquotedCommandFollowedByItsArguments_IsInvalid()
        {
            AssertError("watch get Player", "unexpected token");
        }

        [Test]
        public void Parse_UnknownInnerCommand_IsInvalid()
        {
            AssertError("watch \"nope x\"", "Invalid command");
        }

        [Test]
        public void Parse_CommandStringSwitch_IsChecked()
        {
            AssertParses("then -c \"get Player\"");
            AssertError("then -c get", "Invalid command");
        }

        [Test]
        public void Parse_CommandStringVariadic_ChecksEachElement()
        {
            AssertParses("each ping \"get Player\"");
            AssertError("each ping get", "Invalid command");
        }

        [Test]
        public void Parse_InnerCommand_IsNotCreated()
        {
            ConstructionCounter.Count = 0;

            AssertParses("watch created");

            Assert.That(ConstructionCounter.Count, Is.Zero);
        }

        // --- Aliases ---

        [Test]
        public void Parse_AliasAsCommandString_IsTheWholeExpansion()
        {
            _aliases["health"] = "get -n hp Player";

            Assert.That(ParseValue("watch -i 2 health", "command"), Is.EqualTo(new CommandString("get -n hp Player")));
        }

        [Test]
        public void Parse_QuotedAlias_IsExpandedOnlyByTheInnerCommand()
        {
            _aliases["health"] = "get -n hp Player";

            Assert.That(ParseValue("watch \"health\"", "command"), Is.EqualTo(new CommandString("health")));
        }

        [Test]
        public void Parse_AliasAsCommandName_IsExpandedIntoTokens()
        {
            _aliases["w"] = "watch -i 2";

            Assert.That(ParseValue("w ping", "interval"), Is.EqualTo(2f));
        }

        [Test]
        public void Parse_AliasAsPlainArguments_IsExpandedIntoTokens()
        {
            _aliases["two"] = "1 2";

            Assert.That(ParseValue("pair two", "second"), Is.EqualTo(2));
        }

        [Test]
        public void Parse_AliasExpandingToItself_IsInvalid()
        {
            _aliases["loop"] = "watch loop";

            AssertError("watch loop", "nested more than");
        }

        [Test]
        public void Parse_SingleQuotedTextWithSpaces_KeepsItsSpacesWhenAliasesExist()
        {
            _aliases["unused"] = "ping";

            Assert.That(ParseValue("get 'a  b'", "target"), Is.EqualTo("a  b"));
        }

        private object ParseValue(string line, string field)
        {
            var result = _parser.Parse(line, _ops, _aliases);
            Assert.That(result.TryGetGraph(out var graph), Is.True, result.Message);

            var command = graph.ToArray()[0].Command;
            return command.GetType().GetField(field).GetValue(command);
        }

        private void AssertParses(string line)
        {
            var result = _parser.Parse(line, _ops, _aliases);
            Assert.That(result.TryGetGraph(out _), Is.True, result.Message);
        }

        private void AssertError(string line, string expected)
        {
            var result = _parser.Parse(line, _ops, _aliases);
            Assert.That(result.TryGetGraph(out _), Is.False);
            Assert.That(result.Message, Does.Contain(expected).IgnoreCase);
        }
    }
}
