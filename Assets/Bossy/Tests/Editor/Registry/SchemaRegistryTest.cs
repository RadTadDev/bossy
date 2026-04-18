using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Registry;
using Bossy.Schema;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Registry
{
    /// <summary>
    /// Tests the <see cref="SchemaRegistry"/> class.
    /// </summary>
    internal class SchemaRegistryTest
    {
        [Test]
        public void Test_TryResolveSchema_Roots()
        {
            var name = "root";
            
            var types = new List<Type>();
            for (var i = 0; i < 5; i++)
            {
                types.Add(CommandGenerator.WithName($"{name}_{i}").Generate().GetType());
            }

            var discoverer = new MockCommandDiscoverer(types);
            var graph = CommandDependencyGraphBuilder.BuildGraph(discoverer.GetAllCommandTypes());
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            var registry = new SchemaRegistry(schemas);
            
            for (var i = 0; i < 5; i++)
            {
                Assert.That(registry.TryResolveSchema($"{name}_{i}", out _), Is.EqualTo(SchemaQueryStatus.Found));
            }
            
            Assert.That(registry.TryResolveSchema($"root", out _), Is.EqualTo(SchemaQueryStatus.NotFound));
            Assert.That(registry.TryResolveSchema($"{name}_0 ", out _), Is.EqualTo(SchemaQueryStatus.NotFound));
        }
        
        [Test]
        public void Test_TryResolveSchema_Subcommands()
        {
            var parent = CommandGenerator.WithName("parent").Generate().GetType();
            var child = CommandGenerator.WithName("child").AsSubcommand(parent).Generate().GetType();
            var grandchild = CommandGenerator.WithName("grandchild").AsSubcommand(child).Generate().GetType();

            var types = new List<Type> { parent, child, grandchild };
            
            var discoverer = new MockCommandDiscoverer(types);
            var graph = CommandDependencyGraphBuilder.BuildGraph(discoverer.GetAllCommandTypes());
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            var registry = new SchemaRegistry(schemas);
            
            Assert.That(registry.TryResolveSchema("parent", out _), Is.EqualTo(SchemaQueryStatus.Found));
            Assert.That(registry.TryResolveSchema("parent", new [] { "child" }, out _), Is.EqualTo(SchemaQueryStatus.Found));
            Assert.That(registry.TryResolveSchema("parent",  new [] { "child", "grandchild" }, out _), Is.EqualTo(SchemaQueryStatus.Found));
            
            Assert.That(registry.TryResolveSchema("parent.child", out _), Is.EqualTo(SchemaQueryStatus.NotFound));
            Assert.That(registry.TryResolveSchema(" parent", new [] { "child" }, out _), Is.EqualTo(SchemaQueryStatus.NotFound));
        }

        [Test]
        public void GetValidationResult_Valid()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("test",  "description", type, null);
            var registry = new SchemaRegistry(new [] { schema });
            
            Assert.That(registry.GetValidationResult(schema).IsValid, Is.True);
        }
        
        [Test]
        public void GetValidationResult_Invalid()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("",  "description", type, null);
            var registry = new SchemaRegistry(new [] { schema });
            
            Assert.That(registry.GetValidationResult(schema).IsValid, Is.False);
        }
        
        [Test]
        public void GetInvalidSchemas_Returns_Correct()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("",  "description", type, null);
            var registry = new SchemaRegistry(new [] { schema });
            
            Assert.That(registry.GetInvalidSchemas().ToList(), Is.EquivalentTo(new List<CommandSchema> { schema }));
        }
        
        [Test]
        public void GetInvalidSchemas_ReturnsChild_Correct()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var type2 = CommandGenerator.WithName("test2").Generate().GetType();
            
            var schema1 = new CommandSchema("parent",  "description", type, null);
            var schema2 = new CommandSchema("",  "description", type2, null);

            ICommandSchemaWriter writer = schema1;
            writer.AddChild(schema2);
            writer = schema2;
            writer.SetParent(schema1);
            
            var registry = new SchemaRegistry(new [] { schema1, schema2 });
            
            Assert.That(registry.GetInvalidSchemas().ToList(), Is.EquivalentTo(new List<CommandSchema> { schema2 }));
        }
        
        [Test]
        public void GetValidSchemas_Unfiltered()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var type2 = CommandGenerator.WithName("test2").Generate().GetType();
            
            var schema1 = new CommandSchema("parent",  "description", type, null);
            var schema2 = new CommandSchema("child",  "description", type2, null);

            ICommandSchemaWriter writer = schema1;
            writer.AddChild(schema2);
            
            var registry = new SchemaRegistry(new [] { schema1, schema2 });
            
            Assert.That(registry.GetValidSchemas().ToList(), Is.EquivalentTo(new List<CommandSchema> { schema1, schema2 }));
        }
        
        [Test]
        public void GetValidSchemas_FilteredIncludesRoot()
        {
            var excludeType = CommandGenerator.WithName("exclude").Generate().GetType();
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var type2 = CommandGenerator.WithName("test2").Generate().GetType();
            
            var exclude = new CommandSchema("exclude",  "description", excludeType, null);
            var schema1 = new CommandSchema("parent",  "description", type, null);
            var schema2 = new CommandSchema("child",  "description", type2, null);

            ICommandSchemaWriter writer = schema1;
            writer.AddChild(schema2);
            
            var registry = new SchemaRegistry(new [] { exclude, schema1, schema2 });

            var actual = registry.GetValidSchemas(new[] { "parent" }).ToList();
            
            Assert.That(actual, Is.EquivalentTo(new List<CommandSchema> { schema1, schema2 }));
        }
        
        [Test]
        public void GetValidSchemas_FilteredSkipsParent()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var type1 = CommandGenerator.WithName("test1").Generate().GetType();
            var type2 = CommandGenerator.WithName("test2").Generate().GetType();
            
            var root = new CommandSchema("grandparent",  "description", type, null);
            var schema1 = new CommandSchema("parent",  "description", type1, null);
            var schema2 = new CommandSchema("child",  "description", type2, null);

            ICommandSchemaWriter writer = schema1;
            writer.AddChild(schema2);
            
            var registry = new SchemaRegistry(new [] { root, schema1, schema2 });

            var actual = registry.GetValidSchemas(new[] { "parent" }).ToList();
            
            Assert.That(actual, Is.EquivalentTo(new List<CommandSchema> { schema1, schema2 }));
        }
        
        [Test]
        public void GetValidSchemas_FilteredSkipsInvalid()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var type1 = CommandGenerator.WithName("test1").Generate().GetType();
            var type2 = CommandGenerator.WithName("test2").Generate().GetType();
            
            var root = new CommandSchema("grandparent",  "description", type, null);
            var schema1 = new CommandSchema("parent",  "description", type1, null);
            var schema2 = new CommandSchema("",  "description", type2, null);

            ICommandSchemaWriter writer = schema1;
            writer.AddChild(schema2);
            
            var registry = new SchemaRegistry(new [] { root, schema1, schema2 });

            var actual = registry.GetValidSchemas(new[] { "parent" }).ToList();
            
            Assert.That(actual, Is.EquivalentTo(new List<CommandSchema> { schema1 }));
        }
    }
}