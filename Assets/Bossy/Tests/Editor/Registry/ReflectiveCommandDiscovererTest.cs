using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Bossy.Registry;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Registry
{
	/// <summary>
	/// Tests the <see cref="ReflectiveCommandDiscoverer"/> class.
	/// </summary>
	internal class ReflectiveCommandDiscovererTest 
	{
		[Test]
		public void Test_GetAllCommandTypes_Nominal()
		{
			var ours = new List<Type>();

			for (var i = 0; i < 5; i++)
			{
				ours.Add(CommandGenerator.WithName($"test_cmd_{i}").Generate().GetType());
			}

			var discoverer = new ReflectiveCommandDiscoverer(DynamicAssemblyCache.Assembly);

			var all = discoverer.GetAllCommandTypes();
			
			// Test only for subset since other tests will be creating commands...
			// Just ensure the ones we are certain about do show up
			Assert.That(ours, Is.SubsetOf(all));
		}
		
		[Test]
		public void Test_GetAllCommandTypes_ExcludesNonCommandTypes()
		{
			// Generate a plain type with no CommandAttribute or ICommand
			var type = DynamicAssemblyCache.CreateType(null, "plain_type");

			var discoverer = new ReflectiveCommandDiscoverer(DynamicAssemblyCache.Assembly);
			var all = discoverer.GetAllCommandTypes();

			Assert.That(all.Any(t => t.Name == "plain_type"), Is.False);
		}
		
		[Test]
		public void Test_GetAllCommandTypes_DeduplicatesAssemblies()
		{
			var assembly = DynamicAssemblyCache.Assembly;
			var discoverer = new ReflectiveCommandDiscoverer(assembly, assembly);

			var all = discoverer.GetAllCommandTypes();

			Assert.That(all.Count, Is.EqualTo(all.Distinct().Count()));
		}
		
		[Test]
		public void Test_GetAllCommandTypes_EmptyAssembly_ReturnsEmpty()
		{
			var emptyAssembly = AssemblyBuilder
				.DefineDynamicAssembly(new AssemblyName("Empty"), AssemblyBuilderAccess.Run)
				.DefineDynamicModule("EmptyModule")
				.Assembly;

			var discoverer = new ReflectiveCommandDiscoverer(emptyAssembly);
			var all = discoverer.GetAllCommandTypes();

			Assert.That(all, Is.Empty);
		}
		
		[Test]
		public void Test_GetAllCommandTypes_HandlesException()
		{
			var cmd = CommandGenerator.WithName("test").Generate().GetType();
			
			CommandGenerator.WithName("test_cmd").Generate();
			
            // Create incomplete type by not calling builder.CreateType
			// var builder = DynamicAssemblyCache.CreateType();
   //          
			// ArgumentGenerator.WithName("Arg").WithType(typeof(bool)).AsPositional(builder, 0);
   //          
   //          builder.CreateType();
			//
            
            
            var discoverer = new ReflectiveCommandDiscoverer(DynamicAssemblyCache.Assembly);
  
            IReadOnlyList<Type> all = null;
            Assert.DoesNotThrow(() => all = discoverer.GetAllCommandTypes());
            Assert.That(new List<Type> { cmd }, Is.SubsetOf(all));
            
            // Complete it now so it doesn't royally screw everything else up
		}
	}
}