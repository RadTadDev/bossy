using Bossy.Frontend.Parsing;
using Bossy.Registry;
using Bossy.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bossy
{
    /// <summary>
    /// The top level boss (pun intended). Maybe this is a terrible spot for a joke, especially
    /// seeing as it's the only joke in the whole system (don't think about that too hard).
    /// </summary>
    public class BossyConsole 
    {
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _typeAdapterRegistry;
        private readonly LifecycleManager _lifecycleManager;
        private readonly SettingsManager _settingsManager;
        
        private static readonly string SettingsPath = Application.persistentDataPath + "/bossy/settings.json";
        
        /// <summary>
        /// Creates a new Bossy top level object.
        /// </summary>
        /// <param name="schemaRegistry">The schema registry to attach.</param>
        /// <param name="typeAdapterRegistry">The type adapter registry to attach.</param>
        internal BossyConsole(SchemaRegistry schemaRegistry, TypeAdapterRegistry typeAdapterRegistry)
        {
            // TODO: Make this annoying default a setting. This is disabled because it globally overrides ctrl + backspace input 
            DebugManager.instance.enableRuntimeUI = false;
            
            _schemaRegistry = schemaRegistry;
            _typeAdapterRegistry = typeAdapterRegistry;
            _settingsManager = new SettingsManager(new FileSource(SettingsPath));
            
            _lifecycleManager = new LifecycleManager(schemaRegistry, typeAdapterRegistry, new FileSource(SettingsPath));
        }
    }
}