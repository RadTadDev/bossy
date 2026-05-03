using Bossy.Frontend.Parsing;
using Bossy.Schema.Registry;
using Bossy.Settings;

namespace Bossy
{
    /// <summary>
    /// A state container for Bossy.
    /// </summary>
    public class BossyContext
    {
        public readonly SchemaRegistry SchemaRegistry;
        public readonly TypeAdapterRegistry TypeAdapterRegistry;
        public readonly SettingsManager Settings;
        
        public BossyContext(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, SettingsManager settings)
        {
            SchemaRegistry = schemaRegistry;
            TypeAdapterRegistry = adapterRegistry;
            Settings = settings;
        }
    }
}