using System;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Schema.Registry;
using Bossy.Settings;
using UnityEngine;

#if BOSSY_SRP_CORE
using UnityEngine.Rendering;
#endif

namespace Bossy
{
    /// <summary>
    /// The top level boss (pun intended). Maybe this is a terrible spot for a joke, especially
    /// seeing as it's the only joke in the whole system (don't think about that too hard).
    /// </summary>
    public class BossyConsole : IDisposable
    {
        /// <summary>
        /// Fires when a console is toggled.
        /// </summary>
        public event Action<ConsoleSpace, ConsoleVisibility> OnConsoleToggle;
        
        private static readonly string SettingsPath = Application.persistentDataPath + "/bossy/settings.json";

        private readonly BossyContext _context;
        private readonly RuntimeConsole _runtimeConsole;
#if UNITY_EDITOR
        private readonly EditorConsole _editorConsole;
#endif

        /// <summary>
        /// Creates a new Bossy top level object.
        /// </summary>
        /// <param name="schemaRegistry">The schema registry to attach.</param>
        /// <param name="typeAdapterRegistry">The type adapter registry to attach.</param>
        /// <param name="binder">The binder for resolving objects in commands.</param>
        /// <param name="permissions">The permissions controller.</param>
        internal BossyConsole
        (
            SchemaRegistry schemaRegistry,
            TypeAdapterRegistry typeAdapterRegistry,
            IBossyBinder binder,
            BossyPermissions permissions
        )
        {
#if BOSSY_SRP_CORE
            // TODO: Make this annoying default a setting. This is disabled because it globally overrides ctrl + backspace input 
            DebugManager.instance.enableRuntimeUI = false;
#endif
            
            var settings = new SettingsManager(new FileSource(SettingsPath));
            settings.Load();
            
            var parser = new Parser(schemaRegistry, typeAdapterRegistry);
            _context = new BossyContext(schemaRegistry, typeAdapterRegistry, settings, parser, binder, permissions);
            
            var sessionFactory = new SessionFactory(_context, permissions);
            
            _runtimeConsole = new RuntimeConsole(sessionFactory);
            _runtimeConsole.VisibilityChanged += shown => OnConsoleToggle?.Invoke(ConsoleSpace.Runtime, shown);
            
#if UNITY_EDITOR
            _editorConsole = new EditorConsole(sessionFactory);
            _editorConsole.VisibilityChanged += shown => OnConsoleToggle?.Invoke(ConsoleSpace.Editor, shown);
#endif
        }
        
        /// <summary>
        /// Gets the Bossy context which holds a reference to key subsystems.
        /// </summary>
        /// <returns>The context.</returns>
        public BossyContext GetContext() => _context;
        

        public void Dispose()
        {
            _runtimeConsole?.Dispose();
            _editorConsole?.Dispose();
        }
    }
}
