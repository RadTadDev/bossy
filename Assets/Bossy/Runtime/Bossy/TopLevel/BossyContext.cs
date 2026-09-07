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
        public IBossyBinder Binder;

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
        /// Purposefully not public, intended to be hidden from all commands. Here so that Login can grab it.
        /// </summary>
        private readonly BossyPermissions _permissions;
        
        /// <summary>
        /// Creates a new Bossy context.
        /// </summary>
        /// <param name="schemaRegistry">The schema registry.</param>
        /// <param name="adapterRegistry">The adapter registry.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="binder">The object binder.</param>
        /// <param name="permissions">The Bossy permissions.</param>
        public BossyContext(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, SettingsManager settings, Parser parser, IBossyBinder binder, BossyPermissions permissions)
        {
            Binder = binder;
            SchemaRegistry = schemaRegistry;
            TypeAdapterRegistry = adapterRegistry;
            Settings = settings;
            Parser = parser;
            _permissions = permissions;
        }


        /// <summary>
        /// Attaches a new binder.
        /// </summary>
        /// <param name="binder">The binder to attach.</param>
        public void AttachBinder(IBossyBinder binder)
        {
            Binder = binder;
        }
    }
}