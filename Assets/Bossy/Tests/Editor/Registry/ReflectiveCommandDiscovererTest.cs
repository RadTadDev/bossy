using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Bossy.Schema.Registry;
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

			var discoverer = new ReflectiveCommandDiscoverer(new List<Assembly> { DynamicAssemblyCache.Assembly });

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

			var discoverer = new ReflectiveCommandDiscoverer(new List<Assembly> { DynamicAssemblyCache.Assembly });

			var all = discoverer.GetAllCommandTypes();

			Assert.That(all.Any(t => t.Name == "plain_type"), Is.False);
		}
		
		[Test]
		public void Test_GetAllCommandTypes_DeduplicatesAssemblies()
		{
			var assembly = DynamicAssemblyCache.Assembly;
			var discoverer = new ReflectiveCommandDiscoverer(new List<Assembly> { assembly, assembly });

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

			var discoverer = new ReflectiveCommandDiscoverer(new List<Assembly> { emptyAssembly });
			var all = discoverer.GetAllCommandTypes();

			Assert.That(all, Is.Empty);
		}
	}
}