using Bossy.Shell;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Shell
{
    /// <summary>
    /// Tests the <see cref="SimpleContext"/> class.
    /// </summary>
    public class SimpleContextTest
    {
        [Test]
        public void Test_Write()
        {
            var output = new MockWriteable();
            var context = new SimpleContext(output);
            
            context.Write("hello");
            context.Write("world");

            Assert.AreEqual(output.Log.Count, 2);
            Assert.True(output.Log[0].Equals("hello"));
            Assert.True(output.Log[1].Equals("world"));
        }
    }
}