using System.Collections.Generic;
using System.Reflection;
using Bossy.Command;
using NUnit.Framework;
using Bossy.Frontend.Parsing;
using Bossy.Registry;
using Bossy.Schema;
using Bossy.Tests.Utils;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the <see cref="Parser"/> class.
    /// </summary>
    internal class ParserTest
    {
        private Parser _parser;
        private ParseResult _result;
        private OperatorList _ops = new(";", "&&", "||", "|", "!");
        
        [OneTimeSetUp]
        public void Setup()
        {

            // Fake command types
            var root = CommandGenerator.WithName("root").Generate();
            var child = CommandGenerator.WithName("child").AsSubcommand(root.GetType()).Generate();
            var invalidGrandchild = CommandGenerator.WithName("invalidGrandChild").AsSubcommand(child.GetType()).Generate();
            var invalid = CommandGenerator.WithName("invalid").Generate();
            
            // Fake command schemas
            var rootSchema = new CommandSchema("root", "description", root.GetType(), null);
            var childSchema = new CommandSchema("child", "description", child.GetType(), null);
            var invalidGrandchildSchema = new CommandSchema("_invalidGrandChild", "description", invalidGrandchild.GetType(), null);
            var invalidSchema = new CommandSchema("_invalid", "description", invalid.GetType(), null);

            ICommandSchemaWriter writer = rootSchema;
            writer.AddChild(childSchema);
            writer = childSchema;
            writer.SetParent(rootSchema);
            writer.AddChild(invalidGrandchildSchema);
            writer = invalidGrandchildSchema;
            writer.SetParent(childSchema);
            
            // Specific fake units:
            
            // Switches
            var switches = CommandGenerator.WithName("switches")
                .WithSwitch("sw1", typeof(int), 's')
                .WithSwitch("sw2", typeof(int), 'w')
                .Generate();
            var switchesField1 = switches.GetType().GetField("sw1");
            var switchesField2 = switches.GetType().GetField("sw2");
            var switchesArgs = new HashSet<ArgumentSchema>
            {
                new("sw1", "description", switchesField1, switchesField1.GetCustomAttribute<SwitchAttribute>(), null),
                new("sw2", "description", switchesField2, switchesField2.GetCustomAttribute<SwitchAttribute>(), null),
            };
            var switchesSchema = new CommandSchema("switches", "description", switches.GetType(), switchesArgs);
            
            // SwitchesBool
            var switchesBool = CommandGenerator.WithName("switchesBool")
                .WithSwitch("aopt", typeof(bool), 'a')
                .WithSwitch("bopt", typeof(bool), 'b')
                .Generate();
            var switchesBField1 = switchesBool.GetType().GetField("aopt");
            var switchesBField2 = switchesBool.GetType().GetField("bopt");
            var switchesBArgs = new HashSet<ArgumentSchema>
            {
                new("aopt", "description", switchesBField1, switchesBField1.GetCustomAttribute<SwitchAttribute>(), null),
                new("bopt", "description", switchesBField2, switchesBField2.GetCustomAttribute<SwitchAttribute>(), null),
            };
            var switchesBoolSchema = new CommandSchema("switchesBool", "description", switchesBool.GetType(), switchesBArgs);
            
            // Positionals
            var positionals = CommandGenerator.WithName("positionals")
                .WithPositional("pos1", typeof(int))
                .WithPositional("pos2", typeof(int))
                .Generate();
            var positionalsField1 = positionals.GetType().GetField("pos1");
            var positionalsField2 = positionals.GetType().GetField("pos2");
            var positionalsArgs = new HashSet<ArgumentSchema>
            {
                new("pos1", "description", positionalsField1, positionalsField1.GetCustomAttribute<PositionalAttribute>(), null),
                new("pos2", "description", positionalsField2, positionalsField2.GetCustomAttribute<PositionalAttribute>(), null),
            };
            var positionalsSchema = new CommandSchema("positionals", "description", positionals.GetType(), positionalsArgs);
            
            // Optionals
            var optionals = CommandGenerator.WithName("optionals")
                .WithOptional("opt1", typeof(int))
                .WithOptional("opt2", typeof(int))
                .Generate();
            var optionalsField1 = optionals.GetType().GetField("opt1");
            var optionalsField2 = optionals.GetType().GetField("opt2");
            var optionalsArgs = new HashSet<ArgumentSchema>
            {
                new("opt1", "description", optionalsField1, optionalsField1.GetCustomAttribute<OptionalAttribute>(), null),
                new("opt2", "description", optionalsField2, optionalsField2.GetCustomAttribute<OptionalAttribute>(), null),
            };
            var optionalsSchema = new CommandSchema("optionals", "description", optionals.GetType(), optionalsArgs);
            
            // OptionalAndVariadic
            var optionalAndVariadic = CommandGenerator.WithName("optionals")
                .WithOptional("opt", typeof(int))
                .WithVariadic("vari", typeof(bool))
                .Generate();
            var optionalsAndVariadicField1 = optionalAndVariadic.GetType().GetField("opt");
            var optionalsAndVariadicField2 = optionalAndVariadic.GetType().GetField("vari");
            var optionalsAndVariadicArgs = new HashSet<ArgumentSchema>
            {
                new("opt", "description", optionalsAndVariadicField1, optionalsAndVariadicField1.GetCustomAttribute<OptionalAttribute>(), null),
                new("vari", "description", optionalsAndVariadicField2, optionalsAndVariadicField2.GetCustomAttribute<VariadicAttribute>(), null),
            };
            var optionalsAndVariadicSchema = new CommandSchema("optionalAndVariadic",
                "description",
                optionalAndVariadic.GetType(),
                optionalsAndVariadicArgs);
            
            // Variadic
            var variadic = CommandGenerator.WithName("optionals")
                .WithVariadic("vari", typeof(int))
                .Generate();
            var variadicField = variadic.GetType().GetField("vari");
            var variadicArgs = new HashSet<ArgumentSchema>
            {
                new("vari", "description", variadicField, variadicField.GetCustomAttribute<VariadicAttribute>(), null),
            };
            var variadicSchema = new CommandSchema("variadic", "description", variadic.GetType(), variadicArgs);
            
            // All
            var all = CommandGenerator
                .WithName("all")
                .WithSwitch("sw", typeof(int))
                .WithPositional("pos", typeof(int))
                .WithOptional("opt", typeof(int))
                .WithVariadic("vari", typeof(bool))
                .Generate();

            var swField = all.GetType().GetField("sw");
            var posField = all.GetType().GetField("pos");
            var optField = all.GetType().GetField("opt");
            var variField = all.GetType().GetField("vari");
            
            var allArgs = new HashSet<ArgumentSchema>
            {
                new("sw", "description", swField, swField.GetCustomAttribute<SwitchAttribute>(), null),
                new("pos", "description", posField, posField.GetCustomAttribute<PositionalAttribute>(), null),
                new("opt", "description", optField, optField.GetCustomAttribute<OptionalAttribute>(), null),
                new("vari", "description", variField, variField.GetCustomAttribute<VariadicAttribute>(), null),
            };
            var allSchema = new CommandSchema("all", "description", all.GetType(), allArgs);
            
            var schemas = new List<CommandSchema>
            {
                rootSchema,
                childSchema,
                invalidGrandchildSchema,
                invalidSchema,
                
                switchesSchema,
                switchesBoolSchema,
                positionalsSchema,
                optionalsSchema,
                optionalsAndVariadicSchema,
                variadicSchema,
                allSchema
            };
            
            var schemaRegistry = new SchemaRegistry(schemas);
            var adapterRegistry = new TypeAdapterRegistry();

            adapterRegistry.RegisterAdapter(typeof(int), new IntAdapter());
            adapterRegistry.RegisterAdapter(typeof(float), new FloatAdapter());
            adapterRegistry.RegisterAdapter(typeof(bool), new BoolAdapter());
            
            _parser = new Parser(schemaRegistry, adapterRegistry);
        }
        
        [Test]
        public void Parse_EmptyInput()
        {
            _result = _parser.Parse("", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(EmptyInputError)));
            
            _result = _parser.Parse(" ", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(EmptyInputError)));
        }
        
        [Test]
        public void Parse_BadOperatorPositions()
        {
            _result = _parser.Parse(";root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("; root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("root;", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("root ;", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
        
            _result = _parser.Parse("; root ;", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
        }
        
        [Test]
        public void Parse_BadWindowPositions()
        {
            _result = _parser.Parse("root!cmd2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadWindowOperatorError)));
            
            _result = _parser.Parse("root ! cmd2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadWindowOperatorError)));
        }
        
        [Test]
        public void Parse_ContiguousOperators()
        {
            _result = _parser.Parse("root;;cmd2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ContiguousOperatorsError)));
        }

        [Test]
        public void Parse_ContiguousOperators_SameCharacter()
        {
            _result = _parser.Parse("root|||cmd2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ContiguousOperatorsError)));
        }
        
        [Test]
        public void Parse_NoMatchingCommand()
        {
            _result = _parser.Parse("none", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(NoMatchingCommandError)));
        }
        
        [Test]
        public void Parse_InvalidSchema()
        {
            _result = _parser.Parse("_invalid", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(InvalidSchemaError)));
        }
        
        [Test]
        public void Parse_ChildCommandNotConsideredInvalid_ShouldBeMissing()
        {
            _result = _parser.Parse("_invalidGrandChild", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(NoMatchingCommandError)));
        }
        
        [Test]
        public void Parse_InvalidGrandchild()
        {
            _result = _parser.Parse("root child _invalidGrandChild", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(InvalidSchemaError)));
        }
        
        [Test]
        public void Parse_Switches_ExtraneousSwitch()
        {
            _result = _parser.Parse("switches --name", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(InvalidSwitchError)));
        }
        
        [Test]
        public void Parse_Switches_ExtraneousSwitch_Short()
        {
            _result = _parser.Parse("switches -n", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(InvalidSwitchError)));
        }
        
        [Test]
        public void Parse_Switches_Adapt_Fails()
        {
            _result = _parser.Parse("switches --sw1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(TypeAdaptError)));
        }
        
        [Test]
        public void Parse_Switches_Valid()
        {
            _result = _parser.Parse("switches --sw1 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            
            Assert.That(_result.TryGetGraph(out var graph), Is.True);

            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("sw2").GetValue(cmd), Is.EqualTo(0));
        }
         
        [Test]
        public void Parse_Switches_Short_Valid()
        {
            _result = _parser.Parse("switches -s 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("sw2").GetValue(cmd), Is.EqualTo(0));
        }
        
        [Test]
        public void Parse_Switches_Multiple_Valid()
        {
            _result = _parser.Parse("switches --sw1 1 --sw2 2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("sw2").GetValue(cmd), Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_Switches_Multiple_Short_Valid()
        {
            _result = _parser.Parse("switches -s 1 -w 2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("sw2").GetValue(cmd), Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_Switches_Multiple_ShortAndLong_Valid()
        {
            _result = _parser.Parse("switches --sw1 1 -w 2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("sw2").GetValue(cmd), Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_Switches_AggregatedNotBools_InValid()
        {
            _result = _parser.Parse("switches -sw", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(TypeAdaptError)));
        }
        
        [Test]
        public void Parse_Switches_AggregatedBools_Valid()
        {
            _result = _parser.Parse("switchesBool -ab", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("aopt").GetValue(cmd), Is.True);
            Assert.That(cmdType.GetField("bopt").GetValue(cmd), Is.True);
        }
        
        [Test]
        public void Parse_Positionals_Valid()
        {
            _result = _parser.Parse("positionals 1 2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("pos1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("pos2").GetValue(cmd), Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_Positionals_BadAdapt_Fails()
        {
            _result = _parser.Parse("positionals 1 false", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(TypeAdaptError)));
        }
        
        [Test]
        public void Parse_Positionals_NotEnoughTokens_Fails()
        {
            _result = _parser.Parse("positionals 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(MissingPositionalError)));
        }
        
        [Test]
        public void Parse_OptionalsNone_Valid()
        {
            _result = _parser.Parse("optionals", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out _), Is.True);
        }
        
        [Test]
        public void Parse_OptionalsAll_Valid()
        {
            _result = _parser.Parse("optionals 1 2", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("opt1").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("opt2").GetValue(cmd), Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_OptionalsSome_Valid()
        {
            _result = _parser.Parse("optionals 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("opt1").GetValue(cmd), Is.EqualTo(1));
        }
        
        [Test]
        public void Parse_Optionals_BadAdapt_Fails()
        {
            _result = _parser.Parse("optionals 1 false", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(TypeAdaptError)));
        }
        
        [Test]
        public void Parse_Optionals_BadAdaptWithVariadic_Valid()
        {
            _result = _parser.Parse("optionalAndVariadic 1 true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("opt").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true }));
        }
        
        [Test]
        public void Parse_Variadic_None_Valid()
        {
            _result = _parser.Parse("variadic", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.Empty);
        }
        
        [Test]
        public void Parse_Variadic_One_Valid()
        {
            _result = _parser.Parse("variadic 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { 1 }));
        }
        
        [Test]
        public void Parse_Variadic_Many_Valid()
        {
            _result = _parser.Parse("variadic 1 2 3", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { 1, 2, 3 }));
        }
        
        [Test]
        public void Parse_Variadic_BadType_Invalid()
        {
            _result = _parser.Parse("variadic true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(TypeAdaptError)));
        }
        
        [Test]
        public void Parse_NotVariadic_ExtraToken()
        {
            _result = _parser.Parse("root 1", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(UnexpectedTokensError)));
        }
        
        [Test]
        public void Parse_AllArgs_Valid()
        {
            _result = _parser.Parse("all 1 2 true true true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(0));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("opt").GetValue(cmd), Is.EqualTo(2));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true, true, true }));
        }
        
        [Test]
        public void Parse_AllArgs_SwitchFirst_Valid()
        {
            _result = _parser.Parse("all --sw 10 1 2 true true true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(10));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("opt").GetValue(cmd), Is.EqualTo(2));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true, true, true }));
        }
        
        [Test]
        public void Parse_AllArgs_SwitchBeforeOptional_Valid()
        {
            _result = _parser.Parse("all 1 --sw 10 2 true true true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(10));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("opt").GetValue(cmd), Is.EqualTo(2));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true, true, true }));
        }
        
        [Test]
        public void Parse_AllArgs_SwitchBeforeVariadic_Valid()
        {
            _result = _parser.Parse("all 1 2 --sw 10 true true true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(10));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("opt").GetValue(cmd), Is.EqualTo(2));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true, true, true }));
        }
        
        [Test]
        public void Parse_AllArgs_NoOptionals_Valid()
        {
            _result = _parser.Parse("all 1 --sw 10 true true true", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(10));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
            Assert.That(cmdType.GetField("vari").GetValue(cmd), Is.EquivalentTo(new [] { true, true, true }));
        }
        
        [Test]
        public void Parse_AllArgs_NoVariadic_Valid()
        {
            _result = _parser.Parse("all 1 --sw 10", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            
            var cmd = graph.ToArray()[0].Command;
            var cmdType = cmd.GetType();
            Assert.That(cmdType.GetField("sw").GetValue(cmd), Is.EqualTo(10));
            Assert.That(cmdType.GetField("pos").GetValue(cmd), Is.EqualTo(1));
        }
        
        [Test]
        public void Parse_ProperWindow_Start()
        {
            _result = _parser.Parse("!root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
        }
        
        [Test]
        public void Parse_ProperWindow_End()
        {
            _result = _parser.Parse("root!", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
        }
        
        [Test]
        public void Parse_ThenOperator_Success()
        {
            _result = _parser.Parse("root ; root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(2));
        }
        
        [Test]
        public void AndOperator_Success()
        {
            _result = _parser.Parse("root && root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_OrOperator_Success()
        {
            _result = _parser.Parse("root || root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_PipeOperator_Success()
        {
            _result = _parser.Parse("root | root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(2));
        }
        
        [Test]
        public void Parse_LongPipeline_Success()
        {
            _result = _parser.Parse("root | switches --sw1 5 ; all 1 2 || root && root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(5));
        }
        
        [Test]
        public void Parse_LongPipeline_FrontWindow_Success()
        {
            _result = _parser.Parse("!root | switches --sw1 5 ; all 1 2 || root && root", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(5));
        }
        
        [Test]
        public void Parse_LongPipeline_BackWindow_Success()
        {
            _result = _parser.Parse("root | switches --sw1 5 ; all 1 2 || root && root!", _ops);
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ParseSucceeded)));
            Assert.That(_result.TryGetGraph(out var graph), Is.True);
            Assert.That(graph.ToArray().Length, Is.EqualTo(5));
        }
    }
}