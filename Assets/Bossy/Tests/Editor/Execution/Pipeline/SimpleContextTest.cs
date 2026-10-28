using Bossy.Command;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="SimpleContext"/> class.
    /// </summary>
    internal class SimpleContextTest
    {
        [Test]
        public void Test_Write()
        {
            var output = new MockWriteable();
            var context = new SimpleContext(null, output, null);
            
            context.Write("hello");
            context.Write("world");

            Assert.AreEqual(output.Log.Count, 2);
            Assert.True(output.Log[0].Equals("hello"));
            Assert.True(output.Log[1].Equals("world"));
        }

        [Test]
        public void IsWindowed_InCommandWindowSession_IsTrue()
        {
            var session = MockSession.Create(null, null, new MockEndpoint(), isWindowed: true);
            var context = new SimpleContext(session, new MockWriteable(), null);

            Assert.That(context.IsWindowed(), Is.True);
        }

        [Test]
        public void IsWindowed_InConsoleSession_IsFalse()
        {
            var session = MockSession.Create(null, null, new MockEndpoint());
            var context = new SimpleContext(session, new MockWriteable(), null);

            Assert.That(context.IsWindowed(), Is.False);
        }
    }
}