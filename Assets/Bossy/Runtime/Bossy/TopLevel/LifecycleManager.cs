using System;
using System.Collections.Generic;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Schema.Registry;
using Bossy.Settings;
using Bossy.Session;
using JetBrains.Annotations;
using UnityEngine;

namespace Bossy
{
    internal class LifecycleManager
    {
        private readonly BossyContext _context;
        private readonly Parser _parser;
        private readonly HostManager _hostManager;
        private readonly GlobalInput _globalInput;
        private readonly FrontEndFactory _frontEndFactory;
        [UsedImplicitly] private readonly BossyRuntimeManager _runtimeManager;
        
        private Dictionary<Bridge, LifecycleContainer> _containers = new();

        
        public LifecycleManager(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry, ISettingsSource settingsSource)
        {
            _parser = new Parser(schemaRegistry, adapterRegistry);
            var settings = new SettingsManager(settingsSource);
            settings.Load();
            
            _globalInput = new GlobalInput(settings.BossyInputSettings);
            _globalInput.OnToggleMainHost += OnToggleHostInput;
            
            _frontEndFactory = new FrontEndFactory(_parser, settings.BossyInputSettings, settings.BossyCliSettings);

            _hostManager = new HostManager(this, settings.BossyInputSettings, CreateBossySession);
            _runtimeManager = new BossyRuntimeManager();

            _context = new BossyContext(schemaRegistry, adapterRegistry, settings);
            
            ReconnectEditorSessions();
        }
        
        public void CreateBossySession(FrontendType frontendType, SessionSpace space)
        {
            var content = _frontEndFactory.Create(frontendType);
            
            var bridge = new Bridge(CloseSession, CancelCommand);
            var session = new Session.Session(_context, bridge, CreateCommandSession, space);
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
                host.Initialize(_hostManager, _context.Settings.BossyInputSettings, CreateBossySession, SessionSpace.Edit);
                
                var bridge = new Bridge(CloseSession, CancelCommand);
                var session = new Session.Session(_context, bridge, CreateCommandSession, SessionSpace.Edit);
                
                // TODO: Remember correct view via session serializing system (which does not exist yet)
                var content = new CliUserInterfaceView(_parser, _context.Settings.BossyCliSettings, _context.Settings.BossyInputSettings);
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

        private void CreateCommandSession(Session.Session template, CommandGraph graph)
        {
            var content = _frontEndFactory.Create(FrontendType.CommandDisplay);
            
            var bridge = new Bridge(CloseSession, CancelCommand);
            var session = template.Clone(bridge);
            var viewer = new SessionViewer(bridge, content);
            var container = new LifecycleContainer(session, viewer);
            
            _containers[bridge] = container;
            var host = _hostManager.AssignCommandHost(viewer, session.Space);

            void ReleaseAction() => _hostManager.NotifyFocusLost(host, false);
            
            var signaler = new Signaler(ReleaseAction, bridge);
            content.SetSignaler(signaler);

            // We have set this up as a windowed command, don't detect it again
            graph.Windowed = false;
            container.Start(graph);
        }
    }
}