using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class RuntimeHost : MonoBehaviour, IHost
    {
        public IHostController Controller => _controller;
        
        public SessionSpace Space { get; private set; }

        private HostManager _manager;
        private UIDocument _document;
        
        private RuntimeHostController _controller;
        
        public void Initialize(HostManager manager, Action<FrontendType, SessionSpace> createNewSession, SessionSpace space)
        {
            _manager = manager;
            Space = space;
            
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.sortingOrder = 9998;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
            
            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = settings;

            if (space is SessionSpace.RuntimeCommand)
            {
                settings.sortingOrder = 9999;
            }
            
            _controller = new RuntimeHostController(_document, space);
        }

        public void Open()
        {
            _document.rootVisualElement.style.display = DisplayStyle.Flex;
            _controller.Show();
        }

        public void Hide()
        {
            _document.rootVisualElement.style.display = DisplayStyle.None;
            _controller.Hide();
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            _manager.RequestClose(this, true);
        }
    }
}