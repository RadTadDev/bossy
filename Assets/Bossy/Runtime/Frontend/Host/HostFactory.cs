using System;
using Bossy.Settings;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    /// <summary>
    /// Creates hosts.
    /// </summary>
    internal sealed class HostFactory
    {
        private readonly HostManager _hostManager;
        private readonly BossyInputSettings _inputSettings;
        private readonly Action<FrontendType, SessionSpace> _createNewSession;
        
        /// <summary>
        /// Creates a new factory.
        /// </summary>
        /// <param name="hostManager">The host manager.</param>
        /// <param name="settings">The input settings.</param>
        /// <param name="createNewSession">The create new session hook.</param>
        public HostFactory(HostManager hostManager, BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession)
        {
            _hostManager = hostManager;
            _inputSettings = settings;
            _createNewSession = createNewSession;
        }
        
        /// <summary>
        /// Creates and initializes a new host in a given space.
        /// </summary>
        /// <param name="space">The space to create in.</param>
        /// <returns>The initialized host.</returns>
        public IHost CreateHost(SessionSpace space)
        {
            IHost host = null;
#if UNITY_EDITOR
            if (space is SessionSpace.Edit or SessionSpace.EditCommand)
            {
                host = EditorWindow.CreateWindow<EditorHost>();
            }
#endif
            // Always fallback on runtime host
            if (host == null)
            {
                // Create runtime
                var obj = BossyRuntime.Instance.CreateChildObject("[Bossy Runtime Host]");
                host = obj.AddComponent<RuntimeHost>();
            }
            
            host.Initialize(_hostManager, _inputSettings, _createNewSession, space);
            return host;
        }
    }
}