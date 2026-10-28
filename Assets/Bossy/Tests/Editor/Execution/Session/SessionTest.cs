using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bossy.Execution;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Settings;
using Bossy.Tests.Utils;
using Bossy.Tests.Utils.Commands;
using Bossy.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests how <see cref="Session.RunAsync"/> turns what its endpoint reads into commands.
    /// </summary>
    internal class SessionTest
    {
        private BossyContext _context;
        private BossyPermissions _permissions;

        [SetUp]
        public void Setup()
        {
            var schemas = new SchemaRegistry(new List<CommandSchema>
            {
                new("ping", "description", typeof(PingCommand), null)
            });
            var adapters = new TypeAdapterRegistry();
            var settings = new SettingsManager(new MockSettingsSource(string.Empty));
            var parser = new Parser(schemas, adapters);

            _permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);
            _context = new BossyContext(schemas, adapters, settings, parser, null, _permissions);
        }

        [Test]
        public async Task RunAsync_CommandLine_ParsesAndRuns()
        {
            var endpoint = new MockEndpoint("ping");
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.EqualTo(new[] { PingCommand.Reply }));
        }

        [Test]
        public async Task RunAsync_CommandLine_ExpandsSessionAliases()
        {
            var endpoint = new MockEndpoint("p");
            var session = MockSession.Create(_context, _permissions, endpoint);
            session.Aliases["p"] = "ping";

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.EqualTo(new[] { PingCommand.Reply }));
        }

        [Test]
        public async Task RunAsync_UnparsableLine_WritesErrorAndKeepsReading()
        {
            var endpoint = new MockEndpoint("notacommand", "ping");
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Has.Count.EqualTo(2));
            Assert.That(endpoint.Log[0] as string, Does.Contain("Error:"));
            Assert.That(endpoint.Log[1], Is.EqualTo(PingCommand.Reply));
        }

        [Test]
        public async Task RunAsync_BlankLine_IsSkipped()
        {
            var endpoint = new MockEndpoint("   ", "ping");
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.EqualTo(new[] { PingCommand.Reply }));
        }

        [Test]
        public async Task RunAsync_RecordsOnlyLinesThatParse()
        {
            var endpoint = new MockEndpoint("ping", "notacommand", "   ");
            var session = MockSession.Create(_context, _permissions, endpoint);

            await RunUntilInputEnds(session);

            Assert.That(session.History.Lines, Is.EqualTo(new[] { "ping" }));
        }

        [Test]
        public async Task RunAsync_CommandGraph_RunsAsIs()
        {
            var tracker = new TrackingCommand();
            var graph = CommandGraph
                .Create(false)
                .Execute(tracker, null)
                .Build();
            var endpoint = new MockEndpoint(graph);
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(tracker.WasCalled, Is.True);
            Assert.That(session.History.Lines, Is.Empty);
        }

        [Test]
        public async Task RunAsync_OtherResponse_Throws()
        {
            var endpoint = new MockEndpoint(42);
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.TypeOf<BossyNotAdaptableException>());
        }

        /// <summary>
        /// Runs the session until its endpoint runs out of input, which cancels the read loop.
        /// </summary>
        /// <returns>Any other exception the loop ended with, or null.</returns>
        private static async Task<Exception> RunUntilInputEnds(Session session)
        {
            try
            {
                await session.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // The endpoint ran out of input
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }
    }
}
