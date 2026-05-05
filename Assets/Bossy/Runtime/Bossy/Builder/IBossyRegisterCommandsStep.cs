using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Schema.Registry;
using Bossy.Schema;
using Bossy.Utils;
using UnityEditor.Experimental.GraphView;

namespace Bossy
{
    /// <summary>
    /// Specifies how to discover commands.
    /// </summary>
    public interface IBossyRegisterCommandsStep
    {
        /// <summary>
        /// Searches all loaded assemblies for valid commands.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep Automatically();
        
        /// <summary>
        /// Searches the provided loaded assembly for valid commands.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep InAssembly(Assembly assembly);
        
        /// <summary>
        /// Searches the provided loaded assembly name for valid commands.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep InAssembly(string fullyQualifiedName);
        
        /// <summary>
        /// Searches the provided loaded assemblies for valid commands.
        /// Searches all loaded assemblies for valid commands.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep InAssemblies(IEnumerable<Assembly> assemblies);
        
        /// <summary>
        /// Searches the provided loaded assembly names for valid commands.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep InAssemblies(IEnumerable<string> fullyQualifiedNames);
        
        /// <summary>
        /// Loads only the valid types within the provided list.
        /// </summary>
        /// <returns>The <see cref="TypeAdapter"/> builder.</returns>
        public IBossyRegisterStep FromTypes(IEnumerable<Type> commandTypes);
    }
    
    /// <summary>
    /// Specifies how to discover commands.
    /// </summary>
    internal class BossyRegisterCommandsBuilder : IBossyRegisterCommandsStep
    {
        public IBossyRegisterStep Automatically()
        {
            var finder = new ReflectiveCommandDiscoverer(AppDomain.CurrentDomain.GetAssemblies().ToList());
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterStep InAssembly(Assembly assembly)
        {
            var finder = new ReflectiveCommandDiscoverer(new List<Assembly> { BossyAssembly, assembly });
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterStep InAssembly(string fullyQualifiedName)
        {
            var clientAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == fullyQualifiedName);
            var finder = new ReflectiveCommandDiscoverer(new List<Assembly> { BossyAssembly, clientAssembly });
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterStep InAssemblies(IEnumerable<Assembly> assemblies)
        {
            var list = assemblies.Distinct().ToList();
            list.Add(BossyAssembly);
            var finder = new ReflectiveCommandDiscoverer(list);
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterStep InAssemblies(IEnumerable<string> fullyQualifiedNames)
        {
            var list = new List<Assembly>();
            foreach (var name in fullyQualifiedNames)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == name);
                if (assembly != null)
                {
                    list.Add(assembly);
                }
            }
            
            list.Add(BossyAssembly);
            var finder = new ReflectiveCommandDiscoverer(list);
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterStep FromTypes(IEnumerable<Type> commandTypes)
        {
            var list = new List<Type>();
            foreach (var commandType in commandTypes)
            {
                if (ICommandDiscoverer.IsCommandType(commandType))
                {
                    list.Add(commandType);
                }
                else
                {
                    Log.Warning($"Command type {commandType} is not a command and will not be available to run.");
                }
            }
            
            var finder = new ReflectiveCommandDiscoverer(new List<Assembly> { BossyAssembly });
            list.AddRange(finder.GetAllCommandTypes());
            return NextStep(list);
        }

        private IBossyRegisterStep NextStep(IEnumerable<Type> commandTypes)
        {
            var graph = CommandDependencyGraphBuilder.BuildGraph(commandTypes.ToList());
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            var registry = new SchemaRegistry(schemas);

            return new BossyAdapterBuilder(registry);
        }
        
        private Assembly BossyAssembly => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == "Bossy");
    }
}