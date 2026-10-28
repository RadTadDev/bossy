using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Schema;
using Bossy.Tests.Utils;
using Bossy.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="CommandContext"/> class.
    /// </summary>
    internal class CommandContextTest
    {
        private Session _session;
        private TypeAdapterRegistry _adapterRegistry;
        private BossyContext _context;
        private BossyPermissions _permissions;
        
        [OneTimeSetUp]
        public void Setup()
        {
            _adapterRegistry = new TypeAdapterRegistry();
            _adapterRegistry.RegisterAdapter(typeof(int), new IntAdapter()); 
            _context = new BossyContext(null, _adapterRegistry, null, null, null, null);
            _permissions = new BossyPermissions("", new HashSet<CommandSchema>(), true);
            
            _session = MockSession.Create(_context, _permissions, new MockEndpoint());
        }
        
        [Test]
        public async Task Test_ReadAsync_SameType()
        {
            var items = new List<object> { 1, 2.0f, true };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

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

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

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

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();
            
            Assert.That(first, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_ReadAsync_Fails()
        {
            var items = new List<object> { "test" };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();
            
            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            try
            {
                await ctx.ReadAsync<bool>();
            }
            catch (BossyNotAdaptableException)
            {
                Assert.That(true);
            }
        }
        
        [Test]
        public async Task Test_ReadAsync_RetrySucceeds()
        {
            var items = new List<object> { true, 1 };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, true, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();

            Assert.That(first, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_ReadAsync_RetryShowsAdapterError()
        {
            var items = new List<object> { "x", "5" };
            var reader = new MockReadable(items);
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, true, CancellationToken.None);

            var first = await ctx.ReadAsync<int>();

            Assert.That(first, Is.EqualTo(5));
            Assert.That(writer.Log[0].ToString(), Does.Contain("Expected int"));
        }

        [Test]
        public void Test_ReadAsync_RejectsTrailingInput()
        {
            var reader = new MockReadable(new List<object> { "3 4" });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            var exception = Assert.ThrowsAsync<BossyNotAdaptableException>(async () => await ctx.ReadAsync<int>());

            Assert.That(exception.Message, Does.Contain("Unexpected input \"4\""));
        }

        [Test]
        public void Test_ReadAsync_BoolRequiresExplicitValue()
        {
            var reader = new MockReadable(new List<object> { "" });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            Assert.ThrowsAsync<BossyNotAdaptableException>(async () => await ctx.ReadAsync<bool>());
        }

        [Test]
        public void Test_ReadAsync_RejectsOutOfRangeNumber()
        {
            var reader = new MockReadable(new List<object> { 300 });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            Assert.ThrowsAsync<BossyNotAdaptableException>(async () => await ctx.ReadAsync<byte>());
        }

        [Test]
        public async Task Test_ReadAsync_StringKeepsWholeLine()
        {
            var reader = new MockReadable(new List<object> { "hello there" });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            var first = await ctx.ReadAsync<string>();

            Assert.That(first, Is.EqualTo("hello there"));
        }

        [Test]
        public async Task Test_PromptWithOptions_PicksByNumber()
        {
            var reader = new MockReadable(new List<object> { "2" });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            var choice = await ctx.PromptWithOptions(new[] { new Option<string>("a", "Alpha"), new Option<string>("b", "Beta") });

            Assert.That(choice, Is.EqualTo("b"));
        }

        [Test]
        public async Task Test_PromptWithOptions_RetriesInvalidChoice()
        {
            var reader = new MockReadable(new List<object> { "9", "b" });
            var writer = new MockWriteable();

            var ctx = new CommandContext(_session, _context, reader, writer, false, CancellationToken.None);

            var choice = await ctx.PromptWithOptions(new[] { new Option<string>("a", "Alpha"), new Option<string>("b", "Beta") });

            Assert.That(choice, Is.EqualTo("b"));
        }
    }
}