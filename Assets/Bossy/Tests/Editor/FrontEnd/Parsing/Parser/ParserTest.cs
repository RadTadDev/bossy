using System.Collections.Generic;
using NUnit.Framework;
using Bossy.FrontEnd.Parsing;
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

        [OneTimeSetUp]
        public void Setup()
        {
            var ops = new OperatorList(";", "&&", "||", "|", "!");

            // TODO: Add more tests
            
            var test1 = CommandGenerator.WithName("test").Generate();
            
            var schemas = new List<CommandSchema>
            {
                new CommandSchema("cmd1", "description", test1.GetType(), null)
            };
            
            var schemaRegistry = new SchemaRegistry(schemas);
            var adapterRegistry = new TypeAdapterRegistry();
            
            _parser = new Parser(schemaRegistry, adapterRegistry, ops);
        }
        
        [Test]
        public void Test_EmptyInput()
        {
            _result = _parser.Parse("");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(EmptyInputError)));
            
            _result = _parser.Parse(" ");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(EmptyInputError)));
        }
        
        [Test]
        public void Test_BadOperatorPositions()
        {
            _result = _parser.Parse(";cmd1");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("; cmd1");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("cmd1;");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
            
            _result = _parser.Parse("cmd1 ;");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
        
            _result = _parser.Parse("; cmd1 ;");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadOperatorPositionError)));
        }
        
        [Test]
        public void Test_BadWindowPositions()
        {
            _result = _parser.Parse("cmd1!cmd2");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadWindowOperatorError)));
            
            _result = _parser.Parse("cmd1 ! cmd2");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(BadWindowOperatorError)));
        }
        
        [Test]
        public void Test_ContiguousOperators()
        {
            _result = _parser.Parse("cmd1;;cmd2");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ContiguousOperatorsError)));
            
            _result = _parser.Parse("cmd1|||cmd2");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(ContiguousOperatorsError)));
        }
        
        [Test]
        public void Test_NoMatchingCommand()
        {
            _result = _parser.Parse("none");
            Assert.That(_result.GetType(), Is.EqualTo(typeof(NoMatchingCommandError)));
        }
    }
}