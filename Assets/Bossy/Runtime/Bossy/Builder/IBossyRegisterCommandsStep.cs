using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Registry;
using Bossy.Schema;
using Bossy.Utils;

namespace Bossy
{
    public interface IBossyRegisterCommandsStep
    {
        public IBossyRegisterTypeAdapterStep Automatically();
        public IBossyRegisterTypeAdapterStep InAssembly(Assembly assembly);
        public IBossyRegisterTypeAdapterStep InAssembly(string fullyQualifiedName);
        public IBossyRegisterTypeAdapterStep InAssemblies(IEnumerable<Assembly> assemblies);
        public IBossyRegisterTypeAdapterStep InAssemblies(IEnumerable<string> fullyQualifiedNames);
        public IBossyRegisterTypeAdapterStep FromTypes(IEnumerable<Type> commandTypes);
    }
    
    internal class BossyRegisterCommandsBuilder : IBossyRegisterCommandsStep
    {
        /// <summary>
        /// Automatically finds all commands in all loaded assemblies.
        /// </summary>
        /// <returns></returns>
        public IBossyRegisterTypeAdapterStep Automatically()
        {
            var finder = new ReflectiveCommandDiscoverer(AppDomain.CurrentDomain.GetAssemblies().ToList());
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterTypeAdapterStep InAssembly(Assembly assembly)
        {
            var finder = new ReflectiveCommandDiscoverer(new List<Assembly> { BossyAssembly, assembly });
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterTypeAdapterStep InAssembly(string fullyQualifiedName)
        {
            var clientAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == fullyQualifiedName);
            var finder = new ReflectiveCommandDiscoverer(new List<Assembly> { BossyAssembly, clientAssembly });
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterTypeAdapterStep InAssemblies(IEnumerable<Assembly> assemblies)
        {
            var list = assemblies.Distinct().ToList();
            list.Add(BossyAssembly);
            var finder = new ReflectiveCommandDiscoverer(list);
            return NextStep(finder.GetAllCommandTypes());
        }

        public IBossyRegisterTypeAdapterStep InAssemblies(IEnumerable<string> fullyQualifiedNames)
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

        public IBossyRegisterTypeAdapterStep FromTypes(IEnumerable<Type> commandTypes)
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

        private IBossyRegisterTypeAdapterStep NextStep(IEnumerable<Type> commandTypes)
        {
            var graph = CommandDependencyGraphBuilder.BuildGraph(commandTypes.ToList());
            var schemas = SchemaFactory.BuildCommandSchemas(graph);
            var registry = new SchemaRegistry(schemas);

            return new BossyAdapterBuilder(registry);
        }
        
        private Assembly BossyAssembly => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == "Bossy");
    }
}