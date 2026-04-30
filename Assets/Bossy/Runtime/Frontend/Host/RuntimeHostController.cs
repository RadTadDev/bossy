using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class RuntimeHostController : IHostController
    {
        private readonly List<SessionViewer> _attachedViewers = new();

        public Action NoSessionRemains { get; set;  }

        // TODO: This will change once multiple tabs are allowed
        private VisualElement _rect;
        
        public RuntimeHostController(UIDocument document)
        {
            var hostTree = Resources.Load<VisualTreeAsset>("BossyRuntimeHost");
            document.visualTreeAsset = hostTree;

            var root = document.rootVisualElement;
            _rect = root.Q<VisualElement>("content-area");
            
            // TODO: Sub to buttons for tabs, be able to create more, etc.
        }
        
        public void AddViewer(SessionViewer viewer)
        {
            _attachedViewers.Add(viewer);
            viewer.InitializeView(_rect);
        }

        public IEnumerable<Bridge> GetHostedBridges()
        {
            return _attachedViewers.Select(v => v.Bridge);
        }

        public void Show()
        {
            // TODO: Update to respect open tab
            _attachedViewers[0].Focus();
        }

        public void Hide()
        {
            // TODO: Update to respect open tab
            _attachedViewers[0].Defocus();
        }
    }
}