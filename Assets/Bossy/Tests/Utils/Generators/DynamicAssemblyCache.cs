using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Bossy.Global;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Provides a segregated assembly to hold all generated types.
    /// </summary>
    public class DynamicAssemblyCache : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// The dynamic assembly.
        /// </summary>
        public static Assembly Assembly => _moduleBuilder.Assembly;
        
        private static long _id;
        private static long NextId => (uint)Interlocked.Increment(ref _id);
        private static readonly HashSet<string> _definedTypes = new();
        private static readonly ModuleBuilder _moduleBuilder;
        
        static DynamicAssemblyCache()
        {
            try
            {
                // Create an assembly to hold our generated types
                var name = new AssemblyName("TestDynamicAssembly");
                var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

                // Create a dynamic module in Dynamic Assembly.
                _moduleBuilder = builder.DefineDynamicModule("TestDynamicModule");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to initialize {BossyData.Name} dynamic assembly for testing.", e);
            }
        }

        /// <summary>
        /// Gets a type builder for a custom type.
        /// </summary>
        /// <param name="typeName">The name of the type. If null it will default to a unique name.</param>
        /// <param name="parentType">The type this type inherits from, if unspecified it will be object.</param>
        /// <param name="interfaces">Interfaces that this type implements.</param>
        /// <param name="throwOnDefined">Whether to throw an exception if the typeName is already defined.
        /// If false, the name will be made unique and succeed.</param>
        /// <returns>The builder.</returns>
        public static TypeBuilder CreateType(string typeName = null, Type parentType = null, Type[] interfaces = null, bool throwOnDefined = false)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                typeName = $"TestType_{NextId}";
            }
            else if (_definedTypes.Contains(typeName))
            {
                if (throwOnDefined) throw new ArgumentException($"{typeName} was already defined in the dynamic assembly");
                
                typeName += $"_{NextId}";
            }

            _definedTypes.Add(typeName);
            
            return _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public,
                parentType ?? typeof(object),
                interfaces ?? Type.EmptyTypes
            );
        }
    }
}