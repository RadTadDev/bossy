using System;
using NUnit.Framework;

namespace Bossy.Tests.Utils
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

            ArgumentFieldRecord rec = null;
            Assert.DoesNotThrow(() => rec = ArgumentGenerator.WithName(name).WithType(type).AsSwitch(shortName));
            
            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Type, Is.EqualTo(typeof(int)));
            Assert.That(rec.ConstructorArgs, Contains.Item(shortName));
            Assert.That(rec.ConstructorArgs, Contains.Item(name));
        }
        
        [Test]
        public void Test_AsPositional()
        {
            var name = "test";
            int index = 0;
            var type = typeof(int);

            ArgumentFieldRecord rec = null;
            Assert.DoesNotThrow(() => rec = ArgumentGenerator.WithName(name).WithType(type).AsPositional(index));

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Type, Is.EqualTo(typeof(int)));
            Assert.That(rec.ConstructorArgs, Contains.Item(index));
            Assert.That(rec.ConstructorArgs, Contains.Item(name));
        }
        
        [Test]
        public void Test_AsOptional()
        {
            var name = "test";
            int index = -1;
            var type = typeof(int);
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsOptional(index));
            
            index = 0;
            ArgumentFieldRecord rec = null;
            Assert.DoesNotThrow(() => rec = ArgumentGenerator.WithName(name).WithType(type).AsOptional(index));
            
            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Type, Is.EqualTo(typeof(int)));
            Assert.That(rec.ConstructorArgs, Contains.Item(index));
            Assert.That(rec.ConstructorArgs, Contains.Item(name));
        }
        
        [Test]
        public void Test_AsVariadic()
        {
            var name = "test";
            var type = typeof(int[]);
            
            Assert.DoesNotThrow(() => ArgumentGenerator.WithName(name).WithType(type).AsVariadic());
            
            type = typeof(int);
            ArgumentFieldRecord rec = null;
            Assert.DoesNotThrow(() => rec = ArgumentGenerator.WithName(name).WithType(type).AsVariadic());

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Type, Is.EqualTo(typeof(int[])));
            Assert.That(rec.ConstructorArgs, Contains.Item(name));
        }
    }
}