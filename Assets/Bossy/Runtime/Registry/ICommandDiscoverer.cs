using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bossy.Registry
{
    /// <summary>
    /// Defines a strategy for discovering all command types.
    /// </summary>
    public interface ICommandDiscoverer
    {
        /// <summary>
        /// Reflectively discovers all commands in the codebase.
        /// </summary>
        /// <param name="assemblies">All assemblies to load commands from.</param>
        /// <returns>A list of all discovered command types.</returns>
        public IReadOnlyList<Type> GetAllCommandTypes(params Assembly[] assemblies);
    }
}