using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Execution;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Bossy.Tests.Runtime
{
    /// <summary>
    /// Tests the runtime console in play mode, driven by a simulated keyboard.
    /// </summary>
    internal class RuntimeConsoleTest : InputTestFixture
    {
        private Keyboard _keyboard;
        private RuntimeConsole _console;
        private RuntimeOverlay _overlay;

        public override void Setup()
        {
            base.Setup();
            
            // Destroying is deferred, so an earlier test's overlay can still exist
            foreach (var leftover in Object.FindObjectsByType<RuntimeOverlay>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(leftover.gameObject);
            }
            
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _console = new RuntimeConsole(CreateSessionFactory());
            _overlay = Object.FindAnyObjectByType<RuntimeOverlay>();
        }

        public override void TearDown()
        {
            _console?.Dispose();
            base.TearDown();
        }

        [UnityTest] public IEnumerator Starts_Hidden()
        {
            yield return null;

            Assert.That(_overlay, Is.Not.Null);
            Assert.That(IsConsoleVisible, Is.False);
        }

        [UnityTest] public IEnumerator ToggleKey_ShowsThenHides()
        {
            yield return PressToggleKey();
            Assert.That(IsConsoleVisible, Is.True);
            
            yield return PressToggleKey();
            Assert.That(IsConsoleVisible, Is.False);
        }

        [UnityTest] public IEnumerator ToggleKey_TypesWhileEditingText()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();
            
            Input.value = "Assets";
            yield return PressToggleKey();
            
            Assert.That(IsConsoleVisible, Is.True);
        }

        [UnityTest] public IEnumerator CommandWindow_StaysOpenWhenConsoleHidden()
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

        [UnityTest] public IEnumerator CommandWindow_ShowsCommandOutput()
        {
            yield return PressToggleKey();
            yield return WaitForInputFocus();
            
            yield return Submit("!hello");

            var window = _overlay.WindowLayer[0];
            var lines = window.Query<Label>().ToList().Select(l => l.text);
            Assert.That(lines, Has.Some.Contains("hello"));
        }

        [UnityTest] public IEnumerator Dispose_DestroysOverlay()
        {
            _console.Dispose();
            _console = null;
            yield return null;

            Assert.That(_overlay == null, Is.True);
        }

        private bool IsConsoleVisible => _overlay.ConsoleLayer.resolvedStyle.display == DisplayStyle.Flex;

        private TextField Input => _overlay.ConsoleLayer.Q<TextField>("input-field");

        private IEnumerator PressToggleKey()
        {
            // Queued so the press is read in the next frame's update, as real input is
            Press(_keyboard.slashKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.slashKey, queueEventOnly: true);
            yield return null;
        }

        private IEnumerator WaitForInputFocus()
        {
            // The first frames of play mode can pass before the panel is ready to take focus
            for (var i = 0; i < 120 && !ToggleKey.IsEditingText(_overlay.Root.focusController.focusedElement); i++)
            {
                yield return null;
            }
            
            Assert.That(ToggleKey.IsEditingText(_overlay.Root.focusController.focusedElement), Is.True, "The console input never received focus.");
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
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<System.Type> { typeof(HelloCommand) });
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

    [Command("hello", "Writes hello.")]
    internal class HelloCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("hello");
            return CommandStatus.Ok;
        }
    }
}
