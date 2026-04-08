using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Schema;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Schema
{
    /// <summary>
    /// Tests the <see cref="CommandDependencyGraphBuilder"/> and data classes. 
    /// </summary>
    internal class DependencyGraphTest
    {
        [Test]
        public void Test_NominalGraph()
        {
            var parent = CommandGenerator.WithName("parent").Generate().GetType();
            var child1 = CommandGenerator.WithName("child1").AsSubcommand(parent).Generate().GetType();
            var child2 = CommandGenerator.WithName("child2").AsSubcommand(parent).Generate().GetType();
            var child3 = CommandGenerator.WithName("child3").AsSubcommand(parent).Generate().GetType();
            var grandchild = CommandGenerator.WithName("grandchild").AsSubcommand(child1).Generate().GetType();

            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { parent, child1, child2, child3, grandchild });

            // Parent has all three children
            Assert.That(graph.GetChildren(parent), Is.EquivalentTo(new[] { child1, child2, child3 }));

            // child1 has correct grandchild
            Assert.That(graph.GetChildren(child1), Contains.Item(grandchild));

            // Siblings have no children
            Assert.That(graph.GetChildren(child2), Is.Empty);
            Assert.That(graph.GetChildren(child3), Is.Empty);

            // Grandchild has no children
            Assert.That(graph.GetChildren(grandchild), Is.Empty);

            // Root has no parent
            var parentNode = graph.First(kvp => kvp.Key == parent).Value;
            Assert.That(parentNode.Parent, Is.Null);

            // Children have correct parent
            var child1Node = graph.First(kvp => kvp.Key == child1).Value;
            Assert.That(child1Node.Parent, Is.EqualTo(parent));
        }

        [Test]
        public void Test_InvalidParentType()
        {
            var orphan = CommandGenerator.WithName("orphan").Generate().GetType();
            var nonCommandType = typeof(string); // not a command

            // Generate a command that declares a parent that isn't in the list
            var child = CommandGenerator.WithName("child").AsSubcommand(nonCommandType).Generate().GetType();

            Assert.Throws<ArgumentException>(() =>
                CommandDependencyGraphBuilder.BuildGraph(new List<Type> { orphan, child }));
        }

        [Test]
        public void Test_EmptyGraph()
        {
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type>());
            Assert.That(graph, Is.Empty);
        }
    }
}