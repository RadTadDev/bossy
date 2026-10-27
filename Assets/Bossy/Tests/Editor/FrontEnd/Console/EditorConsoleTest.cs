using System.Collections;
using System.Collections.Generic;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Settings;
using Bossy.Tests.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Bossy.Tests.FrontEnd.Console
{
    /// <summary>
    /// Tests the editor console with real editor windows.
    /// </summary>
    internal class EditorConsoleTest
    {
        private EditorConsole _console;

        [SetUp]
        public void Setup()
        {
            // Editor windows cannot open without a graphics device, such as in -nographics batch mode
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            
            CloseConsoleWindows();

            var schemas = new SchemaRegistry(new List<CommandSchema>());
            var adapters = new TypeAdapterRegistry();
            var settings = new SettingsManager(new MockSettingsSource(string.Empty));
            var parser = new Parser(schemas, adapters);
            var context = new BossyContext(schemas, adapters, settings, parser, null, null);
            var permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);

            _console = new EditorConsole(new SessionFactory(context, permissions));
        }

        [TearDown]
        public void TearDown()
        {
            _console?.Dispose();
            CloseConsoleWindows();
        }

        [UnityTest] public IEnumerator Show_OpensWindowWithSession()
        {
            _console.Show();
            yield return null;

            var window = EditorWindow.GetWindow<ConsoleWindow>();
            Assert.That(window.rootVisualElement.Q("session-view"), Is.Not.Null);
        }

        [UnityTest] public IEnumerator UnknownCommand_InputKeepsFocus()
        {
            _console.Show();
            for (var i = 0; i < 5; i++) yield return null;

            var window = EditorWindow.GetWindow<ConsoleWindow>();
            var input = window.rootVisualElement.Q<TextField>("input-field");
            Assert.That(ToggleKey.IsEditingText(window.rootVisualElement.focusController.focusedElement), Is.True, "Input was not focused to begin with.");

            input.value = "notacommand";
            window.SendEvent(Event.KeyboardEvent("return"));
            for (var i = 0; i < 5; i++) yield return null;

            Assert.That(input.value, Is.Empty, "The line was not submitted.");
            Assert.That(ToggleKey.IsEditingText(window.rootVisualElement.focusController.focusedElement), Is.True);
        }

        [UnityTest] public IEnumerator RecreatedWindow_KeepsSession()
        {
            _console.Show();
            yield return null;

            var first = EditorWindow.GetWindow<ConsoleWindow>();
            var sessionView = first.rootVisualElement.Q("session-view");

            // Simulates Unity replacing a window, as it does when maximizing
            var second = ScriptableObject.CreateInstance<ConsoleWindow>();
            second.Show();
            first.Close();
            yield return null;
            yield return null;

            Assert.That(second.rootVisualElement.Q("session-view"), Is.SameAs(sessionView));
            second.Close();
        }

        [UnityTest] public IEnumerator ClosedWindow_NewWindowGetsNewSession()
        {
            _console.Show();
            yield return null;

            var first = EditorWindow.GetWindow<ConsoleWindow>();
            var sessionView = first.rootVisualElement.Q("session-view");
            first.Close();
            yield return null;
            yield return null;

            _console.Show();
            yield return null;

            var second = EditorWindow.GetWindow<ConsoleWindow>();
            Assert.That(second.rootVisualElement.Q("session-view"), Is.Not.SameAs(sessionView));
        }

        private static void CloseConsoleWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<ConsoleWindow>())
            {
                window.Close();
            }
        }
    }
}
