using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Shell;
using Bossy.Tests.Utils;
using Bossy.Tests.Utils.Commands;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="CommandExecutor"/> class.
    /// </summary>
    internal class CommandExecutorTest
    {
        private Session _session;
        private CommandExecutor _executor;
        
        [OneTimeSetUp]
        public void Setup()
        {
            var registry = new TypeAdapterRegistry();
            registry.RegisterAdapter(typeof(string), new StringAdapter());
            
            var bridge = new Bridge(_ => { }, _ => { });
            _session = new Session(bridge, registry);
            _executor = new CommandExecutor(registry);
        }
        
        [Test]
        public async Task Test_SingleSuccessWrites()
        {
            var reader = new MockReadable(new List<object> { "hello", "world" });
            var writer = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand())
                .Build();
            
            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);
            
            Assert.That(writer.Log, Is.EquivalentTo(new[] { "hello", "world" }));
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
                .Execute(tracker1)
                .Then(tracker2)
                .Then(tracker3)
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
                .Execute(new FailCommand())
                .And(tracker)
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
                .Execute(new SuccessfulCommand())
                .And(tracker)
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
                .Execute(new SuccessfulCommand())
                .Or(tracker)
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
                .Execute(new FailCommand())
                .Or(tracker)
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
                .Execute(infinite)
                .Then(tracker)
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
                .Execute(infinite)
                .Then(tracker)
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
                .Execute(infinite)
                .Then(tracker)
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
                .Execute(new FailCommand())
                .And(new SuccessfulCommand())
                .Or(tracker)
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
                .Execute(new SuccessfulCommand())
                .And(new SuccessfulCommand())
                .Or(tracker)
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
                .Execute(new SuccessfulCommand())
                .And(new FailCommand())
                .Or(tracker)
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
                .Execute(new SuccessfulCommand())
                .Or(new FailCommand())
                .And(tracker)
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
                .Execute(new FailCommand())
                .Or(new FailCommand())
                .And(tracker)
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
                .Execute(new FailCommand())
                .Or(new SuccessfulCommand())
                .And(tracker)
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
                .Execute(new EchoCommand())
                .AndPipeTo(new EchoCommand())
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(writer.Log, Is.EquivalentTo(new[] { "hello", "world", CloseWriterSentinel.Object }));
        }

        [Test]
        public async Task Test_Pipeline_ChainedPipes()
        {
            var reader = new MockReadable(new List<object> { "hello", "world" });
            var writer = new MockWriteable();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand())
                .AndPipeTo(new EchoCommand())
                .AndPipeTo(new EchoCommand())
                .Build();

            await _executor.ExecuteAsync(graph, _session, CancellationToken.None, reader, writer);

            Assert.That(writer.Log, Is.EquivalentTo(new[] { "hello", "world", CloseWriterSentinel.Object }));
        }

        [Test]
        public async Task Test_Pipeline_ThenRunsAfterSuccess()
        {
            var reader = new MockReadable(new List<object> { "hello" });
            var writer = new MockWriteable();

            var tracker = new TrackingCommand();

            var graph = CommandGraph
                .Create(false)
                .Execute(new EchoCommand())
                .AndPipeTo(new EchoCommand())
                .Then(tracker)
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
                .Execute(new EchoCommand())
                .AndPipeTo(new FailCommand())
                .Then(tracker)
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
                .Execute(new EchoCommand())
                .AndPipeTo(new FailCommand())
                .Or(tracker)
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
                .Execute(new EchoCommand())
                .AndPipeTo(new SuccessfulCommand())
                .And(tracker)
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
                .Execute(infinite)
                .AndPipeTo(new EchoCommand())
                .Then(tracker)
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
                .Execute(new InfiniteCommand(null, InfiniteOperation.Read))
                .AndPipeTo(new FailCommand())
                .Then(tracker)
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
                .Execute(new IntReaderCommand())
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
                .Execute(new ThrowsCommand())
                .Build();
        
            await _executor.ExecuteAsync(graph, _session, cts.Token, reader, writer);
        
            Assert.That(writer.Log, Is.Not.Empty);
        }
    }
}