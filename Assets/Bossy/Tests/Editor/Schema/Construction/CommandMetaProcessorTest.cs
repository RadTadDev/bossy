using Bossy.Command;
using NUnit.Framework;

namespace Bossy.Tests.Schema
{
    /// <summary>
    /// Tests the <see cref="CommandMetaProcessor"/> class.
    /// </summary>
    internal class CommandMetaProcessorTest
    {
        private class Fixture
        {
            public int _arg;
            public int _3arg;
            public int Arg;
            public int _Arg;
        }
        
        [Test]
        public void Test_CommandName()
        {
            Assert.That(CommandMetaProcessor.CommandName("Cmd"), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" Cmd"), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" Cmd  "), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName(" CMD "), Is.EqualTo("cmd"));
            Assert.That(CommandMetaProcessor.CommandName("cmd "), Is.EqualTo("cmd"));
        }
        
        [Test]
        public void Test_ArgumentName()
        {
            var type = typeof(Fixture);
            
            var attribute = new SwitchAttribute('t', "description");
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_arg")), Is.EqualTo("arg"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_3arg")), Is.EqualTo("arg"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("Arg")), Is.EqualTo("Arg"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_Arg")), Is.EqualTo("Arg"));

            attribute = new SwitchAttribute('t', "description", " _ARG_ ");
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_arg")), Is.EqualTo("_ARG_"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_3arg")), Is.EqualTo("_ARG_"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("Arg")), Is.EqualTo("_ARG_"));
            Assert.That(CommandMetaProcessor.ArgumentName(attribute, type.GetField("_Arg")), Is.EqualTo("_ARG_"));
        }
        
        [Test]
        public void Test_Description()
        {
            Assert.That(CommandMetaProcessor.Description(null), Is.EqualTo(null));
            Assert.That(CommandMetaProcessor.Description(""), Is.EqualTo(""));
            Assert.That(CommandMetaProcessor.Description(" "), Is.EqualTo(" "));
            Assert.That(CommandMetaProcessor.Description(" test "), Is.EqualTo("Test."));
            Assert.That(CommandMetaProcessor.Description(" 3test "), Is.EqualTo("3test."));
            Assert.That(CommandMetaProcessor.Description(" 3test is a real live description "), Is.EqualTo("3test is a real live description."));
        }
    }
}