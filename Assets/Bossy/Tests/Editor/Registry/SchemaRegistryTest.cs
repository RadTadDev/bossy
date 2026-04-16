using System;
using System.Collections.Generic;
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
                Assert.That(registry.TryResolveSchema($"{name}_{i}", out _));
            }
            
            Assert.That(registry.TryResolveSchema($"root", out _), Is.False);
            Assert.That(registry.TryResolveSchema($"{name}_0 ", out _), Is.False);
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
            
            Assert.That(registry.TryResolveSchema("parent", out _));
            Assert.That(registry.TryResolveSchema("parent", new [] { "child" }, out _));
            Assert.That(registry.TryResolveSchema("parent",  new [] { "child", "grandchild" }, out _));
            
            Assert.That(registry.TryResolveSchema("parent.child", out _), Is.False);
            Assert.That(registry.TryResolveSchema(" parent", new [] { "child" }, out _), Is.False);
        }
    }
}