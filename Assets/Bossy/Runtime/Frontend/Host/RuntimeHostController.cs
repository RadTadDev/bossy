using System;
using System.Linq;
using System.Collections.Generic;
using Bossy.Settings;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class RuntimeHostController : IHostController
    {
        private readonly List<SessionViewer> _attachedViewers = new();

        public Action NoSessionRemains { get; set;  }

        // TODO: This will change once multiple tabs are allowed
        private VisualElement _rect;
        
        public RuntimeHostController(UIDocument document, BossyInputSettings settings, SessionSpace space)
        {
            VisualTreeAsset hostTree;
            if (space is SessionSpace.Runtime)
            {
                hostTree = Resources.Load<VisualTreeAsset>("BossyRuntimeHost");
            }
            else
            {
                hostTree = Resources.Load<VisualTreeAsset>("BossyCommandRuntimeHost");
            }
            
            document.visualTreeAsset = hostTree;
            
            var root = document.rootVisualElement;
            root.style.unityFontDefinition = new StyleFontDefinition(Resources.Load<FontAsset>("Font/JetBrainsMono-Regular"));
            
            _rect = root.Q<VisualElement>("content-area");
            _rect.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (settings.CancelCommand.IsAsserted(evt))
                {
                    _attachedViewers[0]?.Bridge.RequestCancelCommand();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            
            
            if (space is SessionSpace.Runtime)
            {
                // TODO: Sub to buttons for tabs, be able to create more, etc.
            }
            else
            {
                root.Q<Button>("button-close").clicked += () => NoSessionRemains?.Invoke();
                
                var title = root.Q<VisualElement>("title-handle");
                var handle = root.Q<VisualElement>("drag-handle");
                var window = root.Q<VisualElement>("window");
                
                WireDrag(title, window);
                WireResize(handle, window);
            }
        }
        
        public void AddViewer(SessionViewer viewer)
        {
            _attachedViewers.Add(viewer);
            viewer.SetRootAndInitializeView(_rect);
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
        
        private void WireDrag(VisualElement titleBar, VisualElement container)
        {
            Vector2 startPointer = default;
            Vector2 startPosition = default;
            var dragging = false;

            titleBar.RegisterCallback<PointerDownEvent>(evt =>
            {
                dragging = true;
                startPointer = evt.position;
                startPosition = new Vector2(container.resolvedStyle.left, container.resolvedStyle.top);
                titleBar.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            titleBar.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging) return;
                
                var delta = (Vector2)evt.position - startPointer;
                var newLeft = Mathf.Clamp(startPosition.x + delta.x, 0, Screen.width - container.resolvedStyle.width);
                var newTop = Mathf.Clamp(startPosition.y + delta.y, 0, Screen.height - container.resolvedStyle.height);
                container.style.left = newLeft;
                container.style.top = newTop;
                evt.StopPropagation();
            });

            titleBar.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;
                titleBar.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }
        
        private void WireResize(VisualElement handle, VisualElement container)
        {
            Vector2 startPointer = default;
            Vector2 startSize = default;
            var resizing = false;

            const float minWidth = 200f;
            const float minHeight = 150f;

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                resizing = true;
                startPointer = evt.position;
                startSize = new Vector2(container.resolvedStyle.width, container.resolvedStyle.height);
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!resizing) return;
    
                var delta = (Vector2)evt.position - startPointer;
    
                var left = container.resolvedStyle.left;
                var top = container.resolvedStyle.top;
    
                var maxWidth = Screen.width - left;
                var maxHeight = Screen.height - top;
    
                container.style.width = Mathf.Clamp(startSize.x + delta.x, minWidth, maxWidth);
                container.style.height = Mathf.Clamp(startSize.y + delta.y, minHeight, maxHeight);
                evt.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                resizing = false;
                handle.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }
    }
}