using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// The runtime UI: one UI document drawn above the game, with the console panel under a layer
    /// for command windows. Command windows stay visible while the console is hidden.
    /// </summary>
    internal sealed class RuntimeOverlay : MonoBehaviour
    {
        // High enough to draw above most game UI
        private const int SortingOrder = 9999;
        
        // Keeps the UI the same physical size on high and low DPI screens
        private const float ReferenceDpi = 96f;

        /// <summary>
        /// Invoked every frame.
        /// </summary>
        public event Action Updated;

        /// <summary>
        /// The root of the overlay.
        /// </summary>
        public VisualElement Root => _document.rootVisualElement;

        /// <summary>
        /// The panel that holds the console. It slides down from the top of the screen.
        /// </summary>
        public VisualElement ConsolePanel { get; private set; }

        /// <summary>
        /// The layer that holds command windows, drawn above the console.
        /// </summary>
        public VisualElement WindowLayer { get; private set; }

        private UIDocument _document;
        private PanelSettings _panelSettings;

        /// <summary>
        /// Creates the overlay on a new game object that survives scene loads.
        /// </summary>
        /// <returns>The overlay.</returns>
        public static RuntimeOverlay Create()
        {
            var gameObject = new GameObject("[Bossy]");
            DontDestroyOnLoad(gameObject);

            var overlay = gameObject.AddComponent<RuntimeOverlay>();
            overlay.Build();
            return overlay;
        }

        private void Build()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.sortingOrder = SortingOrder;
            _panelSettings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            _panelSettings.referenceDpi = ReferenceDpi;
            _panelSettings.fallbackDpi = ReferenceDpi;
            _panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");

            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = _panelSettings;
            _document.visualTreeAsset = Resources.Load<VisualTreeAsset>("BossyRuntimeOverlay");

            // Let clicks reach the game wherever the overlay draws nothing
            Root.pickingMode = PickingMode.Ignore;
            BossyTheme.ApplyTo(Root);
            Root.AddToClassList("bossy-overlay");

            ConsolePanel = Root.Q("console-panel");
            WindowLayer = Root.Q("window-layer");
        }

        private void Update()
        {
            Updated?.Invoke();
        }

        private void OnDestroy()
        {
            Destroy(_panelSettings);
        }
    }
}
