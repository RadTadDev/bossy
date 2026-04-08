using Bossy.Command;
using NUnit.Framework;

namespace Bossy.Tests.Command
{
    /// <summary>
    /// Tests the <see cref="CommandMetaProcessor"/> class.
    /// </summary>
    internal class CommandMetaProcessorTest
    {
        [Test]
        public void Test_CommandName()
        {
            Assert.That(CommandMetaProcessor.CommandName("Cmd"), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" Cmd"), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" Cmd  "), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" CMD "), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName("cmd "), Is.EqualTo("cmd"));
        }
    }
}