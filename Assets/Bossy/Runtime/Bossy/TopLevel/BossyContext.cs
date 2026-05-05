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
        /// A binder for resolving objects.
        /// </summary>
        public readonly IBossyBinder Binder;

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
        public readonly Parser Parser;

        /// <summary>
        /// Creates a new Bossy context.
        /// </summary>
        /// <param name="schemaRegistry">The schema registry.</param>
        /// <param name="adapterRegistry">The adapter registry.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="binder">The object binder.</param>
        public BossyContext(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, SettingsManager settings, Parser parser, IBossyBinder binder)
        {
            Binder = binder;
            SchemaRegistry = schemaRegistry;
            TypeAdapterRegistry = adapterRegistry;
            Settings = settings;
            Parser = parser;
        }
    }
}