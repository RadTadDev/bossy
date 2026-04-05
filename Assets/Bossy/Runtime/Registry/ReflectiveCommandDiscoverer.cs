using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Utils;

namespace Bossy.Registry
{
    /// <summary>
    /// Uses reflection to discover all command types.
    /// </summary>
    public class ReflectiveCommandDiscoverer : ICommandDiscoverer
    {
        public IReadOnlyList<Type> GetAllCommandTypes(params Assembly[] assemblies)
        {
            return assemblies
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        foreach (var ex in e.LoaderExceptions)
                        {
                            // This normally happens when an assembly references types in an unloaded assembly.
                            Log.Warning($"Failed to load type during command discovery: {ex?.Message}");
                        }

                        return e.Types.Where(t => t != null);
                    }
                })
                .Distinct()
                .Where(IsCommandType).ToList();
        }

        /// <summary>
        /// Tells if a type is a valid command type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if it is a command type, false otherwise.</returns>
        public static bool IsCommandType(Type type)
        {
            return type is { IsAbstract: false, IsInterface: false } &&
                   type.GetCustomAttribute<CommandAttribute>() is not null &&
                   typeof(ICommand).IsAssignableFrom(type);
        }
    }
}