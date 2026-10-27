using System.Collections.Generic;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Settings;
using Bossy.Tests.Utils;
using Bossy.Tests.Utils.Commands;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Bossy.Tests.FrontEnd.Console
{
    /// <summary>
    /// Tests building session views with the CLI frontend, without an editor window.
    /// </summary>
    internal class SessionViewTest
    {
        private SessionFactory _factory;
        private readonly List<SessionView> _views = new();

        [SetUp]
        public void Setup()
        {
            var schemas = new SchemaRegistry(new List<CommandSchema>());
            var adapters = new TypeAdapterRegistry();
            var settings = new SettingsManager(new MockSettingsSource(string.Empty));
            var parser = new Parser(schemas, adapters);
            var context = new BossyContext(schemas, adapters, settings, parser, null, null);
            var permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);

            _factory = new SessionFactory(context, permissions);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var view in _views)
            {
                view.Dispose();
            }
            _views.Clear();
        }

        [Test] public void CreateConsole_BuildsCliInsideRoot()
        {
            var view = Track(_factory.CreateConsole((_, _, _, _) => { }));

            Assert.That(view.Root.Q("cli-root"), Is.Not.Null);
            Assert.That(view.Root.Q<TextField>("input-field"), Is.Not.Null);
            Assert.That(view.Session.Host, Is.SameAs(view));
        }

        [Test] public void CreateConsole_InputRowShown()
        {
            var view = Track(_factory.CreateConsole((_, _, _, _) => { }));

            Assert.That(view.Root.Q("input-row").style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test] public void CreateCommandWindow_InputRowHiddenUntilRead()
        {
            var console = Track(_factory.CreateConsole((_, _, _, _) => { }));
            var graph = CommandGraph.Create(true).Execute(new SuccessfulCommand(), null).Build();

            var view = Track(_factory.CreateCommandWindow(console.Session, graph, (_, _, _, _) => { }));

            Assert.That(view.Root.Q("input-row").style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test] public void PushView_AddsOnTopAndPopRemoves()
        {
            var view = Track(_factory.CreateConsole((_, _, _, _) => { }));
            var childCount = view.Root.childCount;

            view.PushView(new ViewCommand());
            Assert.That(view.Root.childCount, Is.EqualTo(childCount + 1));

            view.PopView();
            Assert.That(view.Root.childCount, Is.EqualTo(childCount));
        }

        [Test] public void OpenCommandWindow_UsesOpener()
        {
            CommandGraph opened = null;
            var view = Track(_factory.CreateConsole((_, graph, _, _) => opened = graph));
            var expected = CommandGraph.Create(true).Execute(new SuccessfulCommand(), null).Build();

            view.OpenCommandWindow(view.Session, expected, null, null);

            Assert.That(opened, Is.SameAs(expected));
        }

        private SessionView Track(SessionView view)
        {
            _views.Add(view);
            return view;
        }
    }
}
