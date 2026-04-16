using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Schema
{
    /// <summary>
    /// Tests the <see cref="CommandSchema"/> class.
    /// </summary>
    internal class CommandSchemaTest
    {
        [Test]
        public void Test_Schema()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("dup1", typeof(bool))
                .WithSwitch("dup2", typeof(bool))
                .Generate().GetType();

            var field1 = type.GetField("dup1");
            var field2 = type.GetField("dup2");
            
            var arg1 =  new ArgumentSchema("dup1", "desc", field1, field1.GetCustomAttribute<ArgumentAttribute>(), null);
            var arg2 =  new ArgumentSchema("dup2", "desc", field2, field2.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg1, arg2 };
            
            var schema = new CommandSchema("test", "description", type, args);
            
            Assert.That(schema.Name, Is.EqualTo("test"));
            Assert.That(schema.CommandType, Is.EqualTo(type));
            Assert.That(schema.Arguments.Count, Is.EqualTo(2));
            Assert.That(schema.Arguments.Any(a => a.Name.Equals("dup1")), Is.True);
            Assert.That(schema.Arguments.Any(a => a.Name.Equals("dup2")), Is.True);
            Assert.That(schema.CommandType, Is.EqualTo(type));
            Assert.That(schema.ChildSchemas, Is.Empty);

            var command = schema.Instantiate();
            
            Assert.That(command, Is.Not.Null);
            Assert.That(command.GetType(), Is.EqualTo(type));

            var attribute = command.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.Name, Is.EqualTo("test"));
            Assert.That(attribute.ParentType, Is.Null);
            
            Assert.That(command.GetType().GetFields().Any(f => f.Name.Equals("dup1")), Is.True);
            Assert.That(command.GetType().GetFields().Any(f => f.Name.Equals("dup2")), Is.True);
        }
        
        private static ArgumentSchema MakeSwitch(string name, char shortName) =>
            new ArgumentSchema(name, "desc", null, new SwitchAttribute(shortName, ""), Array.Empty<ArgumentValidationAttribute>());

        private static ArgumentSchema MakePositional(string name, int index) =>
            new ArgumentSchema(name, "desc", null, new PositionalAttribute(index, ""), Array.Empty<ArgumentValidationAttribute>());

        private static ArgumentSchema MakeOptional(string name, int index) =>
            new ArgumentSchema(name, "desc", null, new OptionalAttribute(index, ""), Array.Empty<ArgumentValidationAttribute>());

        private static ArgumentSchema MakeVariadic(string name) =>
            new ArgumentSchema(name, "desc", null, new VariadicAttribute(""), Array.Empty<ArgumentValidationAttribute>());

        private static CommandSchema MakeSchema(params ArgumentSchema[] args) =>
            new CommandSchema("cmd", "desc", typeof(int), args.ToHashSet());

        // TryFindSwitch(string name)
        [Test] public void FindSwitch_ByName_Found() =>
            Assert.That(MakeSchema(MakeSwitch("verbose", 'v')).TryFindSwitch("verbose", out _), Is.True);

        [Test] public void FindSwitch_ByName_ReturnsSchema() {
            var schema = MakeSchema(MakeSwitch("verbose", 'v'));
            schema.TryFindSwitch("verbose", out var arg);
            Assert.That(arg.Name, Is.EqualTo("verbose"));
        }

        [Test] public void FindSwitch_ByName_NotFound() =>
            Assert.That(MakeSchema(MakeSwitch("verbose", 'v')).TryFindSwitch("missing", out _), Is.False);

        [Test] public void FindSwitch_ByName_NotFound_OutIsNull() {
            MakeSchema(MakeSwitch("verbose", 'v')).TryFindSwitch("missing", out var arg);
            Assert.That(arg, Is.Null);
        }

        [Test] public void FindSwitch_ByName_DoesNotMatchNonSwitch() =>
            Assert.That(MakeSchema(MakePositional("verbose", 0)).TryFindSwitch("verbose", out _), Is.False);

        // TryFindSwitch(char shortName)
        [Test] public void FindSwitch_ByShortName_Found() =>
            Assert.That(MakeSchema(MakeSwitch("verbose", 'v')).TryFindSwitch('v', out _), Is.True);

        [Test] public void FindSwitch_ByShortName_ReturnsSchema() {
            var schema = MakeSchema(MakeSwitch("verbose", 'v'));
            schema.TryFindSwitch('v', out var arg);
            Assert.That(arg.Name, Is.EqualTo("verbose"));
        }

        [Test] public void FindSwitch_ByShortName_NotFound() =>
            Assert.That(MakeSchema(MakeSwitch("verbose", 'v')).TryFindSwitch('x', out _), Is.False);

        // TryFindPositional
        [Test] public void FindPositional_Found() =>
            Assert.That(MakeSchema(MakePositional("target", 0)).TryFindPositional("target", out _), Is.True);

        [Test] public void FindPositional_ReturnsSchema() {
            var schema = MakeSchema(MakePositional("target", 0));
            schema.TryFindPositional("target", out var arg);
            Assert.That(arg.Name, Is.EqualTo("target"));
        }

        [Test] public void FindPositional_NotFound() =>
            Assert.That(MakeSchema(MakePositional("target", 0)).TryFindPositional("missing", out _), Is.False);

        [Test] public void FindPositional_DoesNotMatchSwitch() =>
            Assert.That(MakeSchema(MakeSwitch("target", 't')).TryFindPositional("target", out _), Is.False);

        // TryFindOptional
        [Test] public void FindOptional_Found() =>
            Assert.That(MakeSchema(MakeOptional("output", 0)).TryFindOptional("output", out _), Is.True);

        [Test] public void FindOptional_NotFound() =>
            Assert.That(MakeSchema(MakeOptional("output", 0)).TryFindOptional("missing", out _), Is.False);

        [Test] public void FindOptional_DoesNotMatchPositional() =>
            Assert.That(MakeSchema(MakePositional("output", 0)).TryFindOptional("output", out _), Is.False);

        // TryFindVariadic
        [Test] public void FindVariadic_Found() =>
            Assert.That(MakeSchema(MakeVariadic("files")).TryFindVariadic("files", out _), Is.True);

        [Test] public void FindVariadic_NotFound() =>
            Assert.That(MakeSchema(MakeVariadic("files")).TryFindVariadic("missing", out _), Is.False);

        [Test] public void FindVariadic_DoesNotMatchPositional() =>
            Assert.That(MakeSchema(MakePositional("files", 0)).TryFindVariadic("files", out _), Is.False);
    }
}