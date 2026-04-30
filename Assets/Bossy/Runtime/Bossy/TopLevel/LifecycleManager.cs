using System;
using System.Collections.Generic;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Registry;
using Bossy.Settings;
using Bossy.Shell;
using UnityEngine;

namespace Bossy
{
    internal class LifecycleManager
    {
        private readonly HostManager _hostManager;
        private readonly SessionManager _sessionManager;
        private readonly TypeAdapterRegistry _adapterRegistry;
        private readonly GlobalInput _globalInput;
        private readonly Parser _parser;
        private readonly SettingsManager _settings;
        private readonly FrontEndFactory _frontEndFactory;
        private readonly BossyRuntimeManager _runtimeManager;
        
        private Dictionary<Bridge, LifecycleContainer> _containers = new();

        public LifecycleManager(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, ISettingsSource settingsSource)
        {
            _adapterRegistry = adapterRegistry;
            _parser = new Parser(schemaRegistry, adapterRegistry);

            _settings = new SettingsManager(settingsSource);
            _settings.Load();
            
            _globalInput = new GlobalInput(_settings.BossyInputSettings);
            _globalInput.OnToggleMainHost += OnToggleHostInput;
            
            _frontEndFactory = new FrontEndFactory(_parser, _settings.BossyInputSettings, _settings.BossyCliSettings);
            
            _sessionManager = new SessionManager(this);
            _hostManager = new HostManager(this, CreateBossySession);

            _runtimeManager = new BossyRuntimeManager();
            ReconnectEditorSessions();
        }
        
        public void CreateBossySession(FrontendType frontendType, SessionSpace space)
        {
            var content = _frontEndFactory.Create(frontendType);
            
            var bridge = new Bridge(CloseSession, CancelCommand);
            var session = new Session(bridge, _adapterRegistry);
            var viewer = new SessionViewer(bridge, content);
            var container = new LifecycleContainer(session, viewer);
            
            _containers[bridge] = container;
            var host = _hostManager.AssignHost(viewer, space);

            void ReleaseAction() => _hostManager.NotifyFocusLost(host, false);
            
            var signaler = new Signaler(ReleaseAction, bridge);
            content.SetSignaler(signaler);

            container.Start();
        }
        
        /// <summary>
        /// Handles closing all sessions attached to a host.
        /// </summary>
        /// <param name="host"></param>
        public void HandleHostClosure(IHost host)
        {
            var bridges = host.Controller.GetHostedBridges();
            
            foreach (var bridge in bridges)
            {
                if (!_containers.ContainsKey(bridge))
                {
                    throw new InvalidOperationException(
                        "Attempted to close a session that was not registered with the lifecycle manager!");
                }
                
                _containers[bridge].Close();
            }
        }

        private void OnToggleHostInput(SessionSpace space)
        {
            if (!_hostManager.HasOpenHost(space))
            {
                // TODO: Allow user to specify default view in settings
                CreateBossySession(FrontendType.CommandLine, space);
            }
            else
            {
                _hostManager.ToggleHost(space);
            }
        }
        
        private void CloseSession(Bridge bridge)
        {
            var container = _containers[bridge];
            container.Close();
        }

        private void CancelCommand(Bridge bridge)
        {
            var container = _containers[bridge];
            container.CancelCommand();
        }
        
        private void ReconnectEditorSessions()
        {
#if UNITY_EDITOR
            var hosts = Resources.FindObjectsOfTypeAll<EditorHost>();

            foreach (var host in hosts)
            {
                host.Initialize(_hostManager, CreateBossySession, SessionSpace.Edit);
                
                var bridge = new Bridge(CloseSession, CancelCommand);
                var session = new Session(bridge, _adapterRegistry);
                
                // TODO: Remember correct view via session serializing system (which does not exist yet)
                var content = new CliContentView(_parser, _settings.BossyCliSettings, _settings.BossyInputSettings);
                var viewer = new SessionViewer(bridge, content);
                var container = new LifecycleContainer(session, viewer);
            
                _containers[bridge] = container;
                _hostManager.ReconnectEditor(viewer, host);
            
                void ReleaseAction() => _hostManager.NotifyFocusLost(host, false);
            
                var signaler = new Signaler(ReleaseAction, bridge);
                content.SetSignaler(signaler);
            
                container.Start();
            }
#endif
        }
    }
}