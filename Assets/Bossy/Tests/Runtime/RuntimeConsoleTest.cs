using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Settings;
using Bossy.Tests.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Bossy.Tests.Runtime
{
    /// <summary>
    /// Tests the runtime console in play mode, driven by a simulated keyboard.
    /// </summary>
    /// <remarks>
    /// This does not use <c>InputTestFixture</c>, because its save and restore of the editor's input state is suspected
    /// of leaving Input System action states behind that error on later mouse events in the editor.
    /// </remarks>
    internal class RuntimeConsoleTest
    {
        private Keyboard _keyboard;
        private RuntimeConsole _console;
        private RuntimeOverlay _overlay;
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditorInputBehavior;
        private InputSettings.BackgroundBehavior _previousBackgroundBehavior;

        [SetUp]
        public void Setup()
        {
            // Without these, keys go to the editor when it has focus, and simulated keys are dropped when the game is not focused
            _previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

            // Destroying is deferred, so an earlier test's overlay can still exist
            foreach (var leftover in Object.FindObjectsByType<RuntimeOverlay>())
            {
                Object.DestroyImmediate(leftover.gameObject);
            }

            _keyboard = InputSystem.AddDevice<Keyboard>();

            // The device can be left disabled when the game is not focused, and its events are then dropped
            InputSystem.EnableDevice(_keyboard);

            // Keyboard.current is the device the toggle key is read from, so the simulated keyboard must be it
            _keyboard.MakeCurrent();
            _console = new RuntimeConsole(CreateSessionFactory());
            _overlay = Object.FindAnyObjectByType<RuntimeOverlay>();
        }

        [TearDown]
        public void TearDown()
        {
            _console?.Dispose();
            InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorInputBehavior;
            InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
        }

        [UnityTest]
        public IEnumerator Starts_Hidden()
        {
            yield return null;

            Assert.That(_overlay, Is.Not.Null);
            Assert.That(IsConsoleVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ToggleKey_ShowsThenHides()
        {
            yield return PressToggleKey();
            Assert.That(IsConsoleVisible, Is.True);
            
            yield return PressToggleKey();
            Assert.That(IsConsoleVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ToggleKey_TypesWhileEditingText()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();
            
            Input.value = "Assets";
            yield return PressToggleKey();
            
            Assert.That(IsConsoleVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator UnknownCommand_InputKeepsFocus()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();

            yield return Submit("notacommand");
            for (var i = 0; i < 5; i++) yield return null;

            Assert.That(ToggleKey.IsTextFieldFocused(_overlay.Root.focusController.focusedElement), Is.True);
        }

        [UnityTest]
        public IEnumerator EmptyLine_InputKeepsFocus()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();

            yield return Submit(string.Empty);
            for (var i = 0; i < 5; i++) yield return null;

            Assert.That(ToggleKey.IsTextFieldFocused(_overlay.Root.focusController.focusedElement), Is.True);
        }

        [UnityTest]
        public IEnumerator CommandWindow_StaysOpenWhenConsoleHidden()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();
            
            yield return Submit("!hello");
            Assert.That(_overlay.WindowLayer.childCount, Is.EqualTo(1));

            yield return PressToggleKey();
            Assert.That(IsConsoleVisible, Is.False);
            Assert.That(_overlay.WindowLayer.childCount, Is.EqualTo(1));
            Assert.That(_overlay.WindowLayer.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator CommandWindow_ShowsCommandOutput()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();
            
            yield return Submit("!hello");

            var window = _overlay.WindowLayer[0];
            var lines = window.Query<Label>().ToList().Select(l => l.text);
            Assert.That(lines, Has.Some.Contains("hello"));
        }

        [UnityTest]
        public IEnumerator Hide_PanelNotDisplayedAfterSlidingOut()
        {
            yield return PressToggleKey();
            yield return PressToggleKey();

            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(_overlay.ConsolePanel.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysOverlay()
        {
            _console.Dispose();
            _console = null;
            yield return null;

            Assert.That(_overlay == null, Is.True);
        }

        private bool IsConsoleVisible => _console.IsVisible;

        private TextField Input => _overlay.ConsolePanel.Q<TextField>("input-field");

        private IEnumerator PressToggleKey()
        {
            // Queued so the press is read in the next frame's update, as real input is
            // Must match ToggleKey.Key
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Slash));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator WaitForInputFocus()
        {
            // The first frames of play mode can pass before the panel is ready to take focus
            for (var i = 0; i < 120 && !ToggleKey.IsTextFieldFocused(_overlay.Root.focusController.focusedElement); i++)
            {
                yield return null;
            }
            
            Assert.That(ToggleKey.IsTextFieldFocused(_overlay.Root.focusController.focusedElement), Is.True, "The console input never received focus.");
        }

        private IEnumerator Submit(string line)
        {
            Input.value = line;
            
            using (var evt = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None))
            {
                evt.target = Input;
                Input.SendEvent(evt);
            }

            // Let the command run and its window update
            yield return null;
            yield return null;
        }

        private static SessionFactory CreateSessionFactory()
        {
            var helloType = CommandGenerator.WithName("hello").WithOutput("hello").Generate().GetType();
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<System.Type> { helloType });
            var schemas = new SchemaRegistry(SchemaFactory.BuildCommandSchemas(graph));
            var adapters = new TypeAdapterRegistry();
            var settings = new SettingsManager(new EmptySettingsSource());
            var parser = new Parser(schemas, adapters);
            var context = new BossyContext(schemas, adapters, settings, parser, null, null);
            var permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);

            return new SessionFactory(context, permissions);
        }

        private class EmptySettingsSource : ISettingsSource
        {
            public string LoadJson() => string.Empty;
            public void SaveJson(string json) { }
        }
    }
}
