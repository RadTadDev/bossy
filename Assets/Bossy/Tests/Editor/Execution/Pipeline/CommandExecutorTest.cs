using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Schema;
using Bossy.Tests.Utils;
using Bossy.Tests.Utils.Commands;
using NUnit.Framework;
using UnityEngine;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="CommandExecutor"/> class.
    /// </summary>
    internal class CommandExecutorTest
    {
        private Session _session;
        private CommandExecutor _executor;
        private BossyPermissions _permissions;
        
        [OneTimeSetUp]
        public void Setup()
        {
            var registry = new TypeAdapterRegistry();
            registry.RegisterAdapter(typeof(string), new StringAdapter());
            _permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);
            
            
            var context = new BossyContext(null, registry, null, null, null, null);
            var bridge = new Bridge(_ => { }, _ => { });
            _session = new Session(context, bridge, _permissions, (_, _, _, _) => { }, SessionSpace.Edit);
            _executor = new CommandExecutor(_session, context, _permissions);
        }
        
        [Test]
        public async Task Test_SingleSuccessWrites()
        {
            var reader = new MockReadable(new List<object> { "hello", "world" });
            var writer = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(writer.Log, Is.SupersetOf(new[] { "hello", "world" }));
        }
        
        [Test]
        public async Task Test_ThenOperator()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            var tracker1 = new TrackingCommand();
            var tracker2 = new TrackingCommand();
            var tracker3 = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(tracker1, null)
                .Then(tracker2, null)
                .Then(tracker3, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(tracker1.WasCalled, Is.True);
            Assert.That(tracker2.WasCalled, Is.True);
            Assert.That(tracker3.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_AndOperator_Fails()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new FailCommand(), null)
                .And(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(tracker.WasCalled, Is.False);
        }
       
        [Test]
        public async Task Test_AndOperator_Succeeds()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .And(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_OrOperator_Fails()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .Or(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_OrOperator_Succeeds()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new FailCommand(), null)
                .Or(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_InfiniteDelay_Cancels()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            using var cts = new CancellationTokenSource();

            var tracker = new TrackingCommand();
            var infinite = new InfiniteCommand(() => cts.Cancel(), InfiniteOperation.Delay);
            
            var graph = CommandGraph
                .Create(false)
                .Execute(infinite, null)
                .Then(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);

            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_InfiniteWrite_Cancels()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            using var cts = new CancellationTokenSource();

            var tracker = new TrackingCommand();
            var infinite = new InfiniteCommand(() => cts.Cancel(), InfiniteOperation.Write);
            
            var graph = CommandGraph
                .Create(false)
                .Execute(infinite, null)
                .Then(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);

            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_InfiniteRead_Cancels()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            var infinite = new InfiniteCommand(() => cts.Cancel(), InfiniteOperation.Read);
            
            var graph = CommandGraph
                .Create(false)
                .Execute(infinite, null)
                .Then(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_AndOr_ShortCircuits()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new FailCommand(), null)
                .And(new SuccessfulCommand(), null)
                .Or(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_AndOr_ShortCircuitsLater()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .And(new SuccessfulCommand(), null)
                .Or(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_AndOr_Completes()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .And(new FailCommand(), null)
                .Or(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_OrAnd_ShortCircuits()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new SuccessfulCommand(), null)
                .Or(new FailCommand(), null)
                .And(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_OrAnd_ShortCircuitsLater()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new FailCommand(), null)
                .Or(new FailCommand(), null)
                .And(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.False);
        }
        
        [Test]
        public async Task Test_OrAnd_Completes()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            using var cts = new CancellationTokenSource();
        
            var tracker = new TrackingCommand();
            
            var graph = CommandGraph
                .Create(false)
                .Execute(new FailCommand(), null)
                .Or(new SuccessfulCommand(), null)
                .And(tracker, null)
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_Pipeline_WritesOutputToNextCommand()
        {
            var reader = new MockReadable(new List<object> { "hello", "world" });
            var writer = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new EchoCommand(), null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(writer.Log, Is.SupersetOf(new[] { "hello", "world", CloseWriterSentinel.Object }));
        }

        [Test]
        public async Task Test_Pipeline_ChainedPipes()
        {
            var reader = new MockReadable(new List<object> { "hello", "world" });
            var writer = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new EchoCommand(), null)
                .AndPipeTo(new EchoCommand(), null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(writer.Log, Is.SupersetOf(new[] { "hello", "world" }));
        }

        [Test]
        public async Task Test_Pipeline_ThenRunsAfterSuccess()
        {
            var reader = new MockReadable(new List<object> { "hello" });
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new EchoCommand(), null)
                .Then(tracker, null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_Pipeline_ThenRunsAfterFailure()
        {
            var reader = new MockReadable(new List<object> { "hello" });
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new FailCommand(), null)
                .Then(tracker, null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(tracker.WasCalled, Is.True);
        }

        [Test]
        public async Task Test_Pipeline_OrRunsAfterFailure()
        {
            var reader = new MockReadable(new List<object> { "hello" });
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new FailCommand(), null)
                .Or(tracker, null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_Pipeline_AndRunsAfterSuccess()
        {
            var reader = new MockReadable(new List<object> { "hello" });
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand(), null)
                .AndPipeTo(new SuccessfulCommand(), null)
                .And(tracker, null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(tracker.WasCalled, Is.True);
        }

        [Test]
        public async Task Test_Pipeline_Cancels()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();

            using var cts = new CancellationTokenSource();

            var tracker = new TrackingCommand();
            var infinite = new InfiniteCommand(() => cts.Cancel(), InfiniteOperation.Delay);

            var graph = CommandGraph
                .Create(false)
                .Execute(infinite, null)
                .AndPipeTo(new EchoCommand(), null)
                .Then(tracker, null)
                .Build();

            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);

            Assert.That(tracker.WasCalled, Is.False);
        }

        [Test]
        public async Task Test_Pipeline_FailureCancelsSiblings()
        {
            var reader = new MockReadable();
            var writer = new MockWriteable();
        
            var tracker = new TrackingCommand();
        
            var cts = new CancellationTokenSource();
        
            var graph = CommandGraph
                .Create(false)
                .Execute(new InfiniteCommand(null, InfiniteOperation.Read), null)
                .AndPipeTo(new FailCommand(), null)
                .Then(tracker, null)
                .Build();
        
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(tracker.WasCalled, Is.True);
        }
        
        [Test]
        public async Task Test_Pipeline_ClosedStreamException()
        {
            var reader = new MockReadable(new List<object> { "test" });
            var writer = new MockWriteable();
        
            var cts = new CancellationTokenSource();
        
            var graph = CommandGraph
                .Create(false)
                .Execute(new IntReaderCommand(), null)
                .Build();
        
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(writer.Log, Is.Not.Empty);
        }
        
        [Test]
        public async Task Test_Pipeline_GeneralException()
        {
            var reader = new MockReadable(new List<object> { "test" });
            var writer = new MockWriteable();
        
            var cts = new CancellationTokenSource();
        
            var graph = CommandGraph
                .Create(false)
                .Execute(new ThrowsCommand(), null)
                .Build();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new Regex("ArgumentException:.*"));
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(writer.Log, Is.Not.Empty);
        }
    }
}