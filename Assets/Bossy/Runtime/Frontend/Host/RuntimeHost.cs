using System;
using Bossy.Settings;
using UnityEngine;
using UnityEngine.TextCore.Text;
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
        
        public void Initialize(HostManager manager, BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession, SessionSpace space)
        {
            _manager = manager;
            Space = space;
            
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.sortingOrder = 9998;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
            
            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = panelSettings;

            if (space is SessionSpace.RuntimeCommand)
            {
                panelSettings.sortingOrder = 9999;
            }
            
            _controller = new RuntimeHostController(_document, settings, space);
        }

        public void Reveal()
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