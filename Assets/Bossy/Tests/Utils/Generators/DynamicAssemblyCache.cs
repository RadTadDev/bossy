using System;
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
        private static long _id;
        
        /// <summary>
        /// Gets a globally unique Id.
        /// </summary>
        public static long NextId => (uint)Interlocked.Increment(ref _id);

        /// <summary>
        /// The dynamic module builder. Use this for adding types to the dynamic assembly.
        /// </summary>
        public static ModuleBuilder ModuleBuilder { get; }

        static DynamicAssemblyCache()
        {
            try
            {
                // Create an assembly to hold our generated types
                var name = new AssemblyName("TestDynamicAssembly");
                var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

                // Create a dynamic module in Dynamic Assembly.
                ModuleBuilder = builder.DefineDynamicModule("TestDynamicModule");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to initialize {BossyData.Name} dynamic assembly for testing.", e);
            }
        }
    }
}