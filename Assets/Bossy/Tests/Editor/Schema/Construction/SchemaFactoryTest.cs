using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Schema;
using Bossy.Tests.Utils;
using Bossy.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Schema
{
    /// <summary>
    /// Tests the <see cref="SchemaFactory"/> class.
    /// </summary>
    internal class SchemaFactoryTest
    {
        [Test]
        public void Test_BuildCommandSchemas_Nominal_SingleCommand()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { type });
            
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            Assert.That(schemas.Count, Is.EqualTo(1));
            Assert.That(schemas[0].Name, Is.EqualTo("test"));
            Assert.That(schemas[0].IsRoot, Is.True);
            Assert.That(schemas[0].ParentSchema, Is.Null);
            Assert.That(schemas[0].ChildSchemas, Is.Empty);
        }

        [Test]
        public void Test_BuildCommandSchemas_Nominal_ParentChildRelationship()
        {
            var parent = CommandGenerator.WithName("parent").Generate().GetType();
            var child = CommandGenerator.WithName("child").AsSubcommand(parent).Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { parent, child });
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            var parentSchema = schemas.First(s => s.Name == "parent");
            var childSchema = schemas.First(s => s.Name == "child");
            
            Assert.That(parentSchema.ChildSchemas, Contains.Item(childSchema));
            Assert.That(childSchema.ParentSchema, Is.EqualTo(parentSchema));
            Assert.That(childSchema.IsRoot, Is.False);
        }

        [Test]
        public void Test_BuildCommandSchemas_Nominal_MultipleChildren()
        {
            var parent = CommandGenerator.WithName("parent").Generate().GetType();
            var child1 = CommandGenerator.WithName("child1").AsSubcommand(parent).Generate().GetType();
            var child2 = CommandGenerator.WithName("child2").AsSubcommand(parent).Generate().GetType();
            var child3 = CommandGenerator.WithName("child3").AsSubcommand(parent).Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { parent, child1, child2, child3 });
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            var parentSchema = schemas.First(s => s.Name == "parent");
            
            Assert.That(parentSchema.ChildSchemas.Count, Is.EqualTo(3));
            Assert.That(parentSchema.ChildSchemas.Select(c => c.Name), 
                Is.EquivalentTo(new[] { "child1", "child2", "child3" }));
        }

        [Test]
        public void Test_BuildCommandSchemas_Nominal_DeepHierarchy()
        {
            var root = CommandGenerator.WithName("root").Generate().GetType();
            var child = CommandGenerator.WithName("child").AsSubcommand(root).Generate().GetType();
            var grandchild = CommandGenerator.WithName("grandchild").AsSubcommand(child).Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { root, child, grandchild });
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            var rootSchema = schemas.First(s => s.Name == "root");
            var childSchema = schemas.First(s => s.Name == "child");
            var grandchildSchema = schemas.First(s => s.Name == "grandchild");
            
            Assert.That(rootSchema.ChildSchemas, Contains.Item(childSchema));
            Assert.That(childSchema.ChildSchemas, Contains.Item(grandchildSchema));
            Assert.That(grandchildSchema.ChildSchemas, Is.Empty);
            Assert.That(grandchildSchema.ParentSchema, Is.EqualTo(childSchema));
        }

        [Test]
        public void Test_BuildCommandSchemas_Nominal_WithArguments()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("myswitch", typeof(bool))
                .WithPositional("mypos", typeof(string), 0)
                .Generate()
                .GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { type });
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            var schema = schemas[0];
            Assert.That(schema.Arguments.Count, Is.EqualTo(2));
            Assert.That(schema.Arguments.Any(a => a.Name == "myswitch"), Is.True);
            Assert.That(schema.Arguments.Any(a => a.Name == "mypos"), Is.True);
        }

        [Test]
        public void Test_BuildCommandSchemas_NameCollision_RootLevel()
        {
            var type1 = CommandGenerator.WithName("duplicate").Generate().GetType();
            var type2 = CommandGenerator.WithName("duplicate").Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { type1, type2 });
            
            Assert.Throws<BossyInitializationException>(() => 
                SchemaFactory.BuildCommandSchemas(graph));
        }

        [Test]
        public void Test_BuildCommandSchemas_NameCollision_SiblingLevel()
        {
            var parent = CommandGenerator.WithName("parent").Generate().GetType();
            var child1 = CommandGenerator.WithName("duplicate").AsSubcommand(parent).Generate().GetType();
            var child2 = CommandGenerator.WithName("duplicate").AsSubcommand(parent).Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { parent, child1, child2 });
            
            Assert.Throws<BossyInitializationException>(() => 
                SchemaFactory.BuildCommandSchemas(graph));
        }

        [Test]
        public void Test_BuildCommandSchemas_NameCollision_SameNameDifferentParents_IsAllowed()
        {
            var parent1 = CommandGenerator.WithName("parent1").Generate().GetType();
            var parent2 = CommandGenerator.WithName("parent2").Generate().GetType();
            var child1 = CommandGenerator.WithName("shared").AsSubcommand(parent1).Generate().GetType();
            var child2 = CommandGenerator.WithName("shared").AsSubcommand(parent2).Generate().GetType();
            
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { parent1, parent2, child1, child2 });
            
            Assert.DoesNotThrow(() => 
                SchemaFactory.BuildCommandSchemas(graph));
        }

        [Test]
        public void Test_BuildCommandSchemas_EmptyGraph()
        {
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type>());
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            
            Assert.That(schemas, Is.Empty);
        }
    }
}