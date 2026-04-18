using System;
using Bossy.FrontEnd;
using Bossy.FrontEnd.Parsing;
using Bossy.Shell;

namespace Bossy
{
    /// <summary>
    /// The command console system.
    /// </summary>
    public class BossyConsole : IBossyBuilder
    {
        /// <summary>
        /// The singleton BossyConsole instance.
        /// </summary>
        internal static BossyConsole Instance;
        
        /// <summary>
        /// True if the console has been initialized.
        /// </summary>
        internal static bool IsInitialized => Instance != null;
        
        /// <summary>
        /// Invoked when Bossy is created.
        /// </summary>
        public static Action<BossyConsole> OnInitialize;
        
        private readonly SessionManager _sessionManager;
        private readonly TypeAdapterRegistry _typeAdapterRegistry;

        private BossyConsole()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Multiple Bossy objects instantiated! Only one is allowed.");
            }
            
            _typeAdapterRegistry = new TypeAdapterRegistry();
            _sessionManager = new SessionManager(_typeAdapterRegistry);
        }

        /// <summary>
        /// Create a Bossy console object.
        /// </summary>
        /// <returns>The Bossy builder.</returns>
        public static IBossyBuilder Create()
        {
            return new BossyConsole();
        }
        
        public IBossyBuilder WithTypeAdapter<T>(BaseTypeAdapter<T> adapter)
        {
            _typeAdapterRegistry.RegisterAdapter(typeof(T), adapter);
            return this;
        }

        public BossyConsole Build()
        {
            Instance = this;
            OnInitialize?.Invoke(this);
            return this;
        }

        /// <summary>
        /// Creates a new Bossy session.
        /// </summary>
        /// <param name="type">The type of user interface to attach to the session.</param>
        public void CreateSession(UserInterfaceType type)
        {
            _sessionManager.CreateSession(type);
        }
    }
}