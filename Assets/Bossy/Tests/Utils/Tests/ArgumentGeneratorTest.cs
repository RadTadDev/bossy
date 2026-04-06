using System;
using System.Reflection;
using Bossy.Command;
using NUnit.Framework;

namespace Bossy.Tests.Utils.Tests
{
    /// <summary>
    /// Tests the <see cref="ArgumentGenerator"/> class.
    /// </summary>
    internal class ArgumentGeneratorTest
    {
        [Test]
        public void Test_WithName()
        {
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName("test"));
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName("test123"));
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName("_test123"));
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName("__test123__34"));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName(""));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName("3test"));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName(" "));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName(" test"));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName("test "));
        }
        
        [Test]
        public void Test_WithType()
        {
            var name = "test";
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(typeof(int)));
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(typeof(string)));
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(typeof(Type)));
            // Note - this is not an error unless used with a variadic argument since someone may define an array type adapter
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(typeof(int[])));
            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName(name).WithType(null));
        }
        
        [Test]
        public void Test_AsSwitch()
        {
            var name = "test";
            var shortName = 't';
            var type = typeof(int);
            var typeBuilder = DynamicAssemblyCache.CreateType();
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsSwitch(typeBuilder, shortName));

            var testType = typeBuilder.CreateType();
            
            var field = testType.GetField(name);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(int)));
            
            var attribute = field.GetCustomAttribute<SwitchAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.GetType(), Is.EqualTo(typeof(SwitchAttribute)));
            
            Assert.That(attribute.ShortName, Is.EqualTo(shortName));
            Assert.That(attribute.OverrideName, Is.EqualTo(name));
        }
        
        [Test]
        public void Test_AsPositional()
        {
            var name = "test";
            int index = -1;
            var type = typeof(int);
            var typeBuilder = DynamicAssemblyCache.CreateType();

            Assert.Throws(typeof(ArgumentException), () => ArgumentGenerator.WithName(name).WithType(type).AsPositional(typeBuilder, index));

            index = 0;
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsPositional(typeBuilder, index));

            var testType = typeBuilder.CreateType();
            
            var field = testType.GetField(name);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(int)));
            
            var attribute = field.GetCustomAttribute<PositionalAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.GetType(), Is.EqualTo(typeof(PositionalAttribute)));
            
            Assert.That(attribute.Index, Is.EqualTo(index));
            Assert.That(attribute.OverrideName, Is.EqualTo(name));
        }
        
        [Test]
        public void Test_AsOptional()
        {
            var name = "test";
            int index = -1;
            var type = typeof(int);
            var typeBuilder = DynamicAssemblyCache.CreateType();
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsOptional(typeBuilder, index));

            index = 0;
            typeBuilder = DynamicAssemblyCache.CreateType();
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsOptional(typeBuilder, index));
            
            var testType = typeBuilder.CreateType();
            
            var field = testType.GetField(name);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(int)));
            
            var attribute = field.GetCustomAttribute<OptionalAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.GetType(), Is.EqualTo(typeof(OptionalAttribute)));
            
            Assert.That(attribute.Index, Is.EqualTo(index));
            Assert.That(attribute.OverrideName, Is.EqualTo(name));
        }
        
        [Test]
        public void Test_AsVariadic()
        {
            var name = "test";
            var type = typeof(int[]);
            var typeBuilder = DynamicAssemblyCache.CreateType();
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsVariadic(typeBuilder));

            type = typeof(int);
            typeBuilder = DynamicAssemblyCache.CreateType();
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsVariadic(typeBuilder));
            
            var testType = typeBuilder.CreateType();
            
            var field = testType.GetField(name);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(int[])));
            
            var attribute = field.GetCustomAttribute<VariadicAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.GetType(), Is.EqualTo(typeof(VariadicAttribute)));
            
            Assert.That(attribute.OverrideName, Is.EqualTo(name));
        }
    }
}