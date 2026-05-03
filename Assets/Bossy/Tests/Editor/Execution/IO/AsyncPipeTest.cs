using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Session;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="AsyncPipe"/> class.
    /// </summary>
    internal class AsyncPipeTest
    {
        [Test]
        public async Task Test_Nominal()
        {
            var pipe = new AsyncPipe();
            
            pipe.Write(1);
            pipe.Write("test");
            pipe.Write(false);

            var number = await pipe.ReadAsync(typeof(int), CancellationToken.None);
            var word = await pipe.ReadAsync(typeof(string), CancellationToken.None);
            var boolean = await pipe.ReadAsync(typeof(bool), CancellationToken.None);
            
            Assert.That(number, Is.EqualTo(1));
            Assert.That(word, Is.EqualTo("test"));
            Assert.That(boolean, Is.EqualTo(false));
        }

        private async Task DelayedWriteAsync(AsyncPipe pipe)
        {
            // This doesn't have to be reliable, it's just a smoke test
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            
            pipe.Write(1);
            pipe.Write("test");
            pipe.Write(false);
        }
        
        [Test]
        public async Task Test_OutOfOrder()
        {
            var pipe = new AsyncPipe();

            _ = DelayedWriteAsync(pipe);
            
            var number = await pipe.ReadAsync(typeof(int), CancellationToken.None);
            var word = await pipe.ReadAsync(typeof(string), CancellationToken.None);
            var boolean = await pipe.ReadAsync(typeof(bool), CancellationToken.None);
            
            Assert.That(number, Is.EqualTo(1));
            Assert.That(word, Is.EqualTo("test"));
            Assert.That(boolean, Is.EqualTo(false));
        }

        private async Task CancelRead(AsyncPipe pipe, CancellationToken token)
        {
            await pipe.ReadAsync(null, token);
        }
        
        [Test]
        public void Test_Cancellation()
        {
            var pipe = new AsyncPipe();

            var cts = new CancellationTokenSource();
            
            cts.Cancel();
            Assert.ThrowsAsync(typeof(TaskCanceledException), async () => await CancelRead(pipe, cts.Token));
        }
    }
}