
using System;
using Bossy.Settings;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    internal sealed class HostFactory
    {
        private readonly HostManager _hostManager;
        private readonly BossyInputSettings _inputSettings;
        private readonly Action<FrontendType, SessionSpace> _createNewSession;
        
        public HostFactory(HostManager hostManager, BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession)
        {
            _hostManager = hostManager;
            _inputSettings = settings;
            _createNewSession = createNewSession;
        }
        
        public IHost CreateHost(SessionSpace space)
        {
            IHost host = null;
#if UNITY_EDITOR
            if (space is SessionSpace.Edit)
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