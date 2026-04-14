using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd.Parsing;
using Bossy.Shell;
using Bossy.Tests.Utils;
using Bossy.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="CommandContext"/> class.
    /// </summary>
    public class CommandContextTest
    {
        private global::Bossy.Shell.Shell _shell;
        
        [OneTimeSetUp]
        public void Setup()
        {
            var registry = new TypeAdapterRegistry();
            registry.RegisterAdapter(typeof(int), new IntAdapter()); 
            
            _shell = new global::Bossy.Shell.Shell(registry);
        }
        
        [Test]
        public async Task Test_ReadAsync_SameType()
        {
            var items = new List<object> { 1, 2.0f, true };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_shell, reader, writer, false, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();
            var second = await ctx.ReadAsync<float>();
            var third = await ctx.ReadAsync<bool>();
            
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(2.0f));
            Assert.That(third, Is.True);
        }
        
        [Test]
        public async Task Test_ReadAsync_ImplicitCast()
        {
            var items = new List<object> { 1, 2.5f };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_shell, reader, writer, false, CancellationToken.None);

            var first = await ctx.ReadAsync<double>();
            var second = await ctx.ReadAsync<uint>();
            
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(2.0f));
        }
        
        [Test]
        public async Task Test_ReadAsync_AdapterRegistry()
        {
            var items = new List<object> { "1" };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_shell, reader, writer, false, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();
            
            Assert.That(first, Is.EqualTo(1));
        }
        
        [Test]
        public void Test_ReadAsync_Fails()
        {
            var items = new List<object> { "test" };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_shell, reader, writer, false, CancellationToken.None);

            Assert.ThrowsAsync<BossyNotAdaptableException>(async () => await ctx.ReadAsync<bool>());
        }
        
        [Test]
        public async Task Test_ReadAsync_RetrySucceeds()
        {
            var items = new List<object> { true, 1 };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_shell, reader, writer, true, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();
            
            Assert.That(first, Is.EqualTo(1));
        }
    }
}