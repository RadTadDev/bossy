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
        /// <summary>
        /// The Bossy schema registry.
        /// </summary>
        public readonly SchemaRegistry SchemaRegistry;
        
        /// <summary>
        /// The Bossy type adapter registry.
        /// </summary>
        public readonly TypeAdapterRegistry TypeAdapterRegistry;
        
        /// <summary>
        /// The Bossy settings manager.
        /// </summary>
        public readonly SettingsManager Settings;
        
        /// <summary>
        /// The Bossy parser.
        /// </summary>
        public readonly Parser _parser;
        
        /// <summary>
        /// Creates a new Bossy context.
        /// </summary>
        /// <param name="schemaRegistry">The schema registry.</param>
        /// <param name="adapterRegistry">The adapter registry.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="parser">The parser.</param>
        public BossyContext(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, SettingsManager settings, Parser parser)
        {
            SchemaRegistry = schemaRegistry;
            TypeAdapterRegistry = adapterRegistry;
            Settings = settings;
            _parser = parser;
        }
    }
}