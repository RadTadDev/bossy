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
        public readonly Parser _parser;
        
        public BossyContext(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, SettingsManager settings, Parser parser)
        {
            SchemaRegistry = schemaRegistry;
            TypeAdapterRegistry = adapterRegistry;
            Settings = settings;
            _parser = parser;
        }
    }
}