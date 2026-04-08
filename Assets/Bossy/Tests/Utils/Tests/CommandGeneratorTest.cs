using System;
using System.Reflection;
using Bossy.Command;
using NUnit.Framework;

namespace Bossy.Tests.Utils.Tests
{
    /// <summary>
    /// Tests the <see cref="CommandGenerator"/> class.
    /// </summary>
    public class CommandGeneratorTest
    {
        [Test]
        public void Test_WithName()
        {
            Assert.DoesNotThrow(() => CommandGenerator.WithName("test"));
            Assert.DoesNotThrow(() => CommandGenerator.WithName("test123"));
            Assert.DoesNotThrow(() => CommandGenerator.WithName("_test123"));
            Assert.DoesNotThrow(() => CommandGenerator.WithName("__test123__34"));
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName(""));
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("3test"));
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName(" "));
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName(" test"));
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test "));
        }
        
        [Test]
        public void Test_AsSubcommand()
        {
            var parent = CommandGenerator.WithName("parent").Generate();
            var parentAttribute = parent.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(parent, Is.Not.Null);
            Assert.That(parentAttribute.Name, Is.EqualTo("parent"));
            Assert.That(parentAttribute.ParentType, Is.Null);
            
            var child = CommandGenerator.WithName("child").AsSubcommand(parent.GetType()).Generate();
            var childAttribute = child.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(childAttribute, Is.Not.Null);
            Assert.That(childAttribute.Name, Is.EqualTo("child"));
            Assert.That(childAttribute.ParentType, Is.EqualTo(parent.GetType()));
        }

        [Test]
        public void Test_WithSwitch()
        {
            var name = "";
            var type = typeof(float);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithSwitch(name, type));

            name = "_3";
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithSwitch(name, type)
                .WithSwitch(name, typeof(string)));
            
            name = "duplicate";
            type = null;
            Assert.Throws(typeof(ArgumentNullException), () => CommandGenerator.WithName("test").WithSwitch(name, type));

            type = typeof(int);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithSwitch(name, type)
                 .WithSwitch(name, type));

            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithSwitch(name, type)
                .WithSwitch(name, typeof(string)));
            
            ICommand command = null;
            Assert.DoesNotThrow(() => command = CommandGenerator.WithName("test").WithSwitch(name, type).Generate());

            var fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(int)));
            
            var fieldAttribute = fieldInfo.GetCustomAttribute<SwitchAttribute>();
                
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            Assert.That(fieldAttribute.ShortName, Is.EqualTo(name[0]));

            var overrideShort = 'p';
            Assert.DoesNotThrow(() => command = CommandGenerator.WithName("test").WithSwitch(name, type, overrideShort).Generate());
            
            fieldInfo = command.GetType().GetField(name);
            
            fieldAttribute = fieldInfo.GetCustomAttribute<SwitchAttribute>();
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            Assert.That(fieldAttribute.ShortName, Is.EqualTo(overrideShort));
        }
        
        [Test]
        public void Test_WithPositional()
        {
            var name = "";
            var type = typeof(bool);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithPositional(name, type));
            
            name = "duplicate";
            type = null;
            Assert.Throws(typeof(ArgumentNullException), () => CommandGenerator.WithName("test").WithPositional(name, type, 0));
            
            type = typeof(bool);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithPositional(name, type, 0)
                .WithSwitch(name, type));
            
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithPositional(name, type, 0)
                .WithPositional(name, type, 1));

            
            ICommand command = null;
            FieldInfo fieldInfo;
            PositionalAttribute fieldAttribute;
            
            name = "arg";
            
            // Ensure the auto increment works
            Assert.DoesNotThrow(() => command = CommandGenerator 
                .WithName("test")
                .WithPositional(name + "1", type)
                .WithPositional(name + "2", type)
                .WithPositional(name + "3", type)
                .WithPositional(name + "4", type)
                .WithPositional(name + "5", type)
                .Generate());

            for (var i = 1; i <= 5; i++)
            {
                fieldInfo = command.GetType().GetField(name + i);
                
                Assert.That(fieldInfo, Is.Not.Null);
                Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
                
                fieldAttribute = fieldInfo.GetCustomAttribute<PositionalAttribute>();
                
                Assert.That(fieldAttribute, Is.Not.Null);
                Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name + i));
                Assert.That(fieldAttribute.Index, Is.EqualTo(i - 1));
            }
            
            // Using same index is not a generation problem and should be allowed
            var duplicate = name + "_";
            Assert.DoesNotThrow(() => command = CommandGenerator
                .WithName("test")
                .WithPositional(name, type, 0)
                .WithPositional(duplicate, type, 0).Generate());
            
            fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<PositionalAttribute>();
                
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            Assert.That(fieldAttribute.Index, Is.EqualTo(0));
            
            
            
            fieldInfo = command.GetType().GetField(duplicate);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<PositionalAttribute>();
                
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(duplicate));
            Assert.That(fieldAttribute.Index, Is.EqualTo(0));
        }
        
        [Test]
        public void Test_WithOptional()
        {
            var name = "";
            var type = typeof(bool);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithOptional(name, type));
            
            name = "duplicate";
            type = null;
            Assert.Throws(typeof(ArgumentNullException), () => CommandGenerator.WithName("test").WithOptional(name, type, 0));
            
            type = typeof(bool);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithOptional(name, type, 0)
                .WithSwitch(name, type));
            
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithOptional(name, type, 0)
                .WithOptional(name, type, 1));

            ICommand command = null;
            FieldInfo fieldInfo;
            OptionalAttribute fieldAttribute;
            
            name = "arg";
            
            // Ensure the auto increment works
            Assert.DoesNotThrow(() => command = CommandGenerator 
                .WithName("test")
                .WithOptional(name + "1", type)
                .WithOptional(name + "2", type)
                .WithOptional(name + "3", type)
                .WithOptional(name + "4", type)
                .WithOptional(name + "5", type)
                .Generate());

            for (var i = 1; i <= 5; i++)
            {
                fieldInfo = command.GetType().GetField(name + i);
                
                Assert.That(fieldInfo, Is.Not.Null);
                Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
                
                fieldAttribute = fieldInfo.GetCustomAttribute<OptionalAttribute>();
                
                Assert.That(fieldAttribute, Is.Not.Null);
                Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name + i));
                Assert.That(fieldAttribute.Index, Is.EqualTo(i - 1));
            }
            
            // Using same index is not a generation problem and should be allowed
            var duplicate = name + "_";
            Assert.DoesNotThrow(() => command = CommandGenerator
                .WithName("test")
                .WithOptional(name, type, 0)
                .WithOptional(duplicate, type, 0).Generate());
            
            fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<OptionalAttribute>();
                
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            Assert.That(fieldAttribute.Index, Is.EqualTo(0));
            
            
            
            fieldInfo = command.GetType().GetField(duplicate);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<OptionalAttribute>();
                
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(duplicate));
            Assert.That(fieldAttribute.Index, Is.EqualTo(0));
        }
        
        [Test]
        public void Test_WithVariadic()
        {
            var name = "";
            var type = typeof(float);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator.WithName("test").WithVariadic(name, type));

            name = "arg";
            type = null;
            Assert.Throws(typeof(ArgumentNullException), () => CommandGenerator.WithName("test").WithVariadic(name, type));
            
            type =  typeof(float);
            Assert.Throws(typeof(ArgumentException), () => CommandGenerator
                .WithName("test")
                .WithVariadic(name, type)
                .WithVariadic(name, type));
            
            ICommand command = null;
            
            // Test nominal case
            Assert.DoesNotThrow(() => command = CommandGenerator.WithName("test").WithVariadic(name, type).Generate());
            
            var fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(float[])));
            
            var fieldAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            
            // In type generation we are free to illegally add multiple variadic containers
            var duplicate = name + "_";
            Assert.DoesNotThrow(() => command = CommandGenerator
                .WithName("test")
                .WithVariadic(name, type)
                .WithVariadic(duplicate, type)
                .Generate());
                    
            fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(float[])));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
            
            
            
            fieldInfo = command.GetType().GetField(duplicate);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(float[])));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(duplicate));
            
            
            // We are also free to illegally pass in an array type - it is automatically upgraded to 2D
            Assert.DoesNotThrow(() => command = CommandGenerator
                .WithName("test")
                .WithVariadic(name, typeof(int[]))
                .Generate());
            
            fieldInfo = command.GetType().GetField(name);
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(int[][])));
            
            fieldAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(fieldAttribute, Is.Not.Null);
            Assert.That(fieldAttribute.OverrideName, Is.EqualTo(name));
        }

        [Test]
        public void Test_SmokeTest()
        {
            ICommand parent = null;
            ICommand child = null;
            ICommand grandchild = null;
            
            Assert.DoesNotThrow(() => parent = CommandGenerator
                .WithName("parent")
                .Generate());
            
            Assert.DoesNotThrow(() => child = CommandGenerator
                .WithName("child")
                .AsSubcommand(parent.GetType())
                .WithOptional("opt", typeof(int))
                .WithVariadic("var", typeof(string))
                .Generate());
            
            Assert.DoesNotThrow(() => grandchild = CommandGenerator
                .WithName("grandchild")
                .AsSubcommand(child.GetType())
                .WithSwitch("swi",  typeof(bool))
                .WithSwitch("swi2",  typeof(int), 'l')
                .WithPositional("pos", typeof(double))
                .WithOptional("opt", typeof(int))
                .WithVariadic("var", typeof(string))
                .Generate());
            
            // Test parent
            var attribute = parent.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.Name, Is.EqualTo("parent"));
            Assert.That(attribute.ParentType, Is.Null);
            
            // Test child
            
            attribute = child.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.Name, Is.EqualTo("child"));
            Assert.That(attribute.ParentType, Is.EqualTo(parent.GetType()));

            
            var fieldInfo = child.GetType().GetField("opt");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(int)));
            
            var optionalAttribute = fieldInfo.GetCustomAttribute<OptionalAttribute>();
            
            Assert.That(optionalAttribute, Is.Not.Null);
            Assert.That(optionalAttribute.OverrideName, Is.EqualTo("opt"));
            Assert.That(optionalAttribute.Index, Is.EqualTo(0));


            fieldInfo = child.GetType().GetField("var");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(string[])));
            
            var variadicAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(variadicAttribute, Is.Not.Null);
            Assert.That(variadicAttribute.OverrideName, Is.EqualTo("var"));
            
            // Test grandchild
            
            attribute = grandchild.GetType().GetCustomAttribute<CommandAttribute>();
            
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.Name, Is.EqualTo("grandchild"));
            Assert.That(attribute.ParentType, Is.EqualTo(child.GetType()));

            fieldInfo = grandchild.GetType().GetField("swi");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(bool)));
            
            var switchAttribute = fieldInfo.GetCustomAttribute<SwitchAttribute>();
            
            Assert.That(switchAttribute, Is.Not.Null);
            Assert.That(switchAttribute.OverrideName, Is.EqualTo("swi"));
            Assert.That(switchAttribute.ShortName, Is.EqualTo('s'));
            
            
            fieldInfo = grandchild.GetType().GetField("swi2");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(int)));
            
            switchAttribute = fieldInfo.GetCustomAttribute<SwitchAttribute>();
            
            Assert.That(switchAttribute, Is.Not.Null);
            Assert.That(switchAttribute.OverrideName, Is.EqualTo("swi2"));
            Assert.That(switchAttribute.ShortName, Is.EqualTo('l'));
            
            
            fieldInfo = grandchild.GetType().GetField("pos");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(double)));
            
            var positionalAttribute = fieldInfo.GetCustomAttribute<PositionalAttribute>();
            
            Assert.That(positionalAttribute, Is.Not.Null);
            Assert.That(positionalAttribute.OverrideName, Is.EqualTo("pos"));
            Assert.That(positionalAttribute.Index, Is.EqualTo(0));
            
            
            fieldInfo = grandchild.GetType().GetField("opt");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(int)));
            
            optionalAttribute = fieldInfo.GetCustomAttribute<OptionalAttribute>();
            
            Assert.That(optionalAttribute, Is.Not.Null);
            Assert.That(optionalAttribute.OverrideName, Is.EqualTo("opt"));
            Assert.That(optionalAttribute.Index, Is.EqualTo(0));


            fieldInfo = grandchild.GetType().GetField("var");
            
            Assert.That(fieldInfo, Is.Not.Null);
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(string[])));
            
            variadicAttribute = fieldInfo.GetCustomAttribute<VariadicAttribute>();
            
            Assert.That(variadicAttribute, Is.Not.Null);
            Assert.That(variadicAttribute.OverrideName, Is.EqualTo("var"));
        }
    }
}