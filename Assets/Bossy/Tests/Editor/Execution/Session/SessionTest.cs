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
    /// <remarks>
    /// The parser creates its own command instances, so a run is observed through the endpoint instead: it is reset
    /// once for every command group that starts, and a line that fails to parse writes an error to it.
    /// </remarks>
    internal class SessionTest
    {
        private const string CommandName = "ping";

        private BossyContext _context;
        private BossyPermissions _permissions;

        [OneTimeSetUp]
        public void Setup()
        {
            // A generated type, because a [Command] class in this assembly would be discovered by the real console
            var commandType = CommandGenerator.WithName(CommandName).Generate().GetType();
            var schemas = new SchemaRegistry(new List<CommandSchema>
            {
                new(CommandName, "description", commandType, null)
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
            var endpoint = new MockEndpoint(CommandName);
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.Empty);
            Assert.That(endpoint.ResetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_CommandLine_ExpandsSessionAliases()
        {
            var endpoint = new MockEndpoint("p");
            var session = MockSession.Create(_context, _permissions, endpoint);
            session.Aliases["p"] = CommandName;

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.Empty);
            Assert.That(endpoint.ResetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_UnparsableLine_WritesErrorAndKeepsReading()
        {
            var endpoint = new MockEndpoint("notacommand", CommandName);
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Has.Count.EqualTo(1));
            Assert.That(endpoint.Log[0] as string, Does.Contain("Error:"));
            Assert.That(endpoint.ResetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_BlankLine_IsSkipped()
        {
            var endpoint = new MockEndpoint("   ", CommandName);
            var session = MockSession.Create(_context, _permissions, endpoint);

            var error = await RunUntilInputEnds(session);

            Assert.That(error, Is.Null);
            Assert.That(endpoint.Log, Is.Empty);
            Assert.That(endpoint.ResetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_RecordsLinesThatFailToParseButNotBlankLines()
        {
            var endpoint = new MockEndpoint(CommandName, "notacommand", "   ");
            var session = MockSession.Create(_context, _permissions, endpoint);

            await RunUntilInputEnds(session);

            Assert.That(session.History.Lines, Is.EqualTo(new[] { CommandName, "notacommand" }));
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
