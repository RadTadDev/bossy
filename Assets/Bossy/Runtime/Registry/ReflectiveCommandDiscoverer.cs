using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Utils;
using UnityEngine;

namespace Bossy.Registry
{
    /// <summary>
    /// Uses reflection to discover all command types.
    /// </summary>
    internal class ReflectiveCommandDiscoverer : ICommandDiscoverer
    {
        private readonly Assembly[] _assemblies;
        
        /// <summary>
        /// Creates a reflective command discoverer.
        /// </summary>
        /// <param name="first">An assembly to search for command types.</param>
        /// <param name="rest">Additional assemblies to search for command types.</param>
        public ReflectiveCommandDiscoverer(Assembly first, params Assembly[] rest)
        {
            _assemblies = new [] { first }.Concat(rest).ToArray();
        }
        
        public IReadOnlyList<Type> GetAllCommandTypes()
        {
            return _assemblies
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
                .Where(ICommandDiscoverer.IsCommandType).ToList();
        }
    }
}