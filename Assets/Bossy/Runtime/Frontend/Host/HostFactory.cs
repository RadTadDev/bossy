
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    internal sealed class HostFactory
    {
        private readonly Action<FrontendType, SessionSpace> _createNewSession;
        private readonly HostManager _hostManager;
        
        public HostFactory(HostManager hostManager, Action<FrontendType, SessionSpace> createNewSession)
        {
            _hostManager = hostManager;
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
            
            host.Initialize(_hostManager, _createNewSession, space);
            return host;
        }
    }
}