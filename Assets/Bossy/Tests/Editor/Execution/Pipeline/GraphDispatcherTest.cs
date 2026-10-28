using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Schema;
using Bossy.Tests.Utils;
using Bossy.Tests.Utils.Commands;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="GraphDispatcher"/> behavior that a session sees: windows, views, endpoint resets and closing.
    /// </summary>
    internal class GraphDispatcherTest
    {
        private BossyPermissions _permissions;
        private BossyContext _context;

        [OneTimeSetUp]
        public void Setup()
        {
            var registry = new TypeAdapterRegistry();
            registry.RegisterAdapter(typeof(string), new StringAdapter());
            _permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);
            _context = new BossyContext(null, registry, null, null, null, null);
        }

        [Test]
        public async Task Test_Windowed_WithHost_OpensCommandWindow()
        {
            var host = new MockSessionHost();
            var session = MockSession.Create(_context, _permissions, new MockEndpoint(), host);

            var graph = CommandGraph
                .Create(true)
                .Execute(new TrackingCommand(), null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None);

            Assert.That(host.Calls, Is.EqualTo(new[] { "open" }));
            Assert.That(host.OpenedGraph, Is.SameAs(graph));
        }

        [Test]
        public async Task Test_Windowed_NoHost_RunsInlineWithWarning()
        {
            var endpoint = new MockEndpoint();
            var session = MockSession.Create(_context, _permissions, endpoint);
            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(true)
                .Execute(tracker, null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None);

            Assert.That(tracker.WasCalled, Is.True);
            Assert.That(endpoint.Log.OfType<string>().Any(line => line.Contains("Command windows are not available")), Is.True);
        }

        [Test]
        public async Task Test_CommandView_PushedAndPopped()
        {
            var host = new MockSessionHost();
            var session = MockSession.Create(_context, _permissions, new MockEndpoint(), host);

            var graph = CommandGraph
                .Create(false)
                .Execute(new ViewCommand(), null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None, new MockReadable(), new MockWriteable());

            Assert.That(host.Calls, Is.EqualTo(new[] { "push", "pop" }));
        }

        [Test]
        public async Task Test_VisualEndpoint_ResetAfterEachGroup()
        {
            var endpoint = new MockEndpoint();
            var session = MockSession.Create(_context, _permissions, endpoint);

            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None);

            Assert.That(endpoint.ResetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_CommandOutput_DoesNotCloseEndpoint()
        {
            var endpoint = new MockEndpoint();
            var session = MockSession.Create(_context, _permissions, endpoint);

            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None);

            Assert.That(endpoint.Log, Has.None.SameAs(CloseWriterSentinel.Object));
        }

        [Test]
        public async Task Test_CallerOutput_IsClosedAfterGraph()
        {
            var session = MockSession.Create(_context, _permissions, new MockEndpoint());
            var output = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .Build();

            await session.ExecuteAsync(graph, CancellationToken.None, new MockReadable(), output);

            Assert.That(output.Log, Has.Some.SameAs(CloseWriterSentinel.Object));
        }
    }
}
