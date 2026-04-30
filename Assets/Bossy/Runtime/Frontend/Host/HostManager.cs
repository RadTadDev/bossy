using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Utils;

namespace Bossy.Frontend
{
    internal class HostManager
    {
        // True if any host is currently being interacted with
        private bool _hostHasFocus;
        
        private readonly HostFactory _hostFactory;
        private readonly LifecycleManager _lifeCycle;
        
        private readonly Action<FrontendType, SessionSpace> _createNewSession;

        private readonly List<IHost> _allHosts = new();
        private readonly Dictionary<SessionSpace, IHost> _mainHosts = new();
        
        public HostManager(LifecycleManager lifeCycle, Action<FrontendType, SessionSpace> createNewSession)
        {
            _lifeCycle = lifeCycle;
            _createNewSession = createNewSession;
            _hostFactory = new HostFactory(this, createNewSession);
        }
        
        public bool HasOpenHost(SessionSpace space) => _mainHosts.ContainsKey(space);

        public void ToggleHost(SessionSpace space)
        {
            if (_hostHasFocus)
            {
                _mainHosts[space].Hide();
                _hostHasFocus = false;
            }
            else
            {
                _hostHasFocus = true;
                _mainHosts[space].Open();
            }
        }
        
        /// <summary>
        /// Assigns a host to a session viewer.
        /// </summary>
        /// <param name="viewer">The viewer to host.</param>
        /// <param name="space">The space to host in.</param>
        /// <returns>The host that was assigned.</returns>
        public IHost AssignHost(SessionViewer viewer, SessionSpace space)
        {
            // Hardcodes assumption that editor windows never have multi-tab capabilities
            if (!_mainHosts.TryGetValue(space, out var host) || space is SessionSpace.Edit)
            {
                host = MakeAndFocusHost(space, viewer);
            }
            
            return host;
        }
        
        public void ReconnectEditor(SessionViewer viewer, EditorHost host)
        {
            host.Initialize(this, _createNewSession, SessionSpace.Edit);
            host.Controller.AddViewer(viewer);
            _mainHosts[host.Space] = host;
            _allHosts.Add(host);
        }
        
        public void NotifyFocusTaken(IHost host)
        {
            // TODO: Possible bug: If editor window is focused, then another editor window is clicked, 
            // it is apparently possible to received the OnFocus event for the new one before the LostFocus event
            // meaning our focus bool will be wrong
            
            _hostHasFocus = true;
            _mainHosts[host.Space] = host;
        }

        public void NotifyFocusLost(IHost host, bool hostAlreadyHidden)
        {
            // TODO: Possible bug: If editor window is focused, then another editor window is clicked, 
            // it is apparently possible to received the OnFocus event for the new one before the LostFocus event
            // meaning our focus bool will be wrong
            
            _hostHasFocus = false;
            if (!hostAlreadyHidden)
            {
                host.Hide();
            }
        }
        
        public void RequestClose(IHost host, bool hostAlreadyClosed)
        {
            _lifeCycle.HandleHostClosure(host);

            if (_mainHosts[host.Space] == host)
            {
                _mainHosts.Remove(host.Space);
            }
            
            NotifyFocusLost(host, hostAlreadyClosed);

            if (!hostAlreadyClosed)
            {
                host.Close();
            }
            
            _allHosts.Remove(host);

            var next = _allHosts.FirstOrDefault(h => h.Space == host.Space);
            if (next != null)
            {
                _mainHosts[host.Space] = next;
            }
        }

        private IHost MakeAndFocusHost(SessionSpace space, SessionViewer viewer = null)
        {
            var host = _hostFactory.CreateHost(space);
            _allHosts.Add(host);
            
            host.Controller.NoSessionRemains += () => RequestClose(host, false);

            if (viewer != null)
            {
                host.Controller.AddViewer(viewer);
            }
            
            NotifyFocusTaken(host);
            
            return host;
        }
    }
}