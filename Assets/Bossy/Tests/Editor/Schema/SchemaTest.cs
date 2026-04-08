using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Editor.Schema
{
    /// <summary>
    /// Tests the <see cref="CommandSchema"/> class.
    /// </summary>
    internal class SchemaTest
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
    }
}