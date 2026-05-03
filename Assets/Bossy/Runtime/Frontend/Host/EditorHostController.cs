using System;
using System.Collections.Generic;
using Bossy.Settings;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class EditorHostController : IHostController
    {
        private SessionViewer _sessionViewer;

        private VisualElement _contentRect;
        
        public EditorHostController(BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession, VisualElement root)
        {
            var tree = Resources.Load<VisualTreeAsset>("BossyEditorHost");

            // This is needed when reconnecting existing editor views
            root.Clear();
            root.style.unityFontDefinition = new StyleFontDefinition(Resources.Load<FontAsset>("Font/JetBrainsMono-Regular"));
            
            tree.CloneTree(root);
            root.Q<Button>("button-close").clicked += () => NoSessionRemains?.Invoke();
            
            // TODO: Eventually need to decide here using current window type
            root.Q<Button>("button-new").clicked += () => createNewSession?.Invoke(FrontendType.CommandLine, SessionSpace.Edit);
            
            _contentRect = root.Q<VisualElement>("content-area");
            
            _contentRect.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (settings.CancelCommand.IsAsserted(evt))
                {
                    _sessionViewer?.Bridge.RequestCancelCommand();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
        }

        public Action NoSessionRemains { get; set; }
        
        public void AddViewer(SessionViewer viewer)
        {
            // Clear existing UI
            _contentRect.Clear();
            
            _sessionViewer = viewer;
            _sessionViewer.SetRootAndInitializeView(_contentRect);
        }

        public IEnumerable<Bridge> GetHostedBridges()
        {
            return new[] { _sessionViewer.Bridge };
        }
        
        public void Show()
        {
            _sessionViewer?.Focus();
        }

        public void Hide()
        {
            _sessionViewer?.Defocus();
        }
    }
}