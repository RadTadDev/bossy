using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Schema.Registry;
using Bossy.Execution;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Registry
{
    /// <summary>
    /// Tests the default methods on the <see cref="ICommandDiscoverer"/> interface.
    /// </summary>
    internal class ICommandDiscovererTest
    {
        private class MissingAttribute : ICommand
        {
            public Task<CommandStatus> ExecuteAsync(CommandContext ctx)
            {
                // Don't execute me :( !
                return Task.FromResult(default(CommandStatus));
            }
        }
        
        [Command("fake", "I won't show in the registry because I'm not a command")]
        private class MissingInheritance { }

        private interface IInterfacesAreNotCommands : ICommand { }

        private abstract class NeitherAreAbstractClasses : ICommand
        {
            public Task<CommandStatus> ExecuteAsync(CommandContext ctx)
            {
                // Don't execute me either :( !
                return Task.FromResult(default(CommandStatus));
            }
        }

        [Test]
        public void Test_IsCommandType()
        {
            Assert.False(ICommandDiscoverer.IsCommandType(null));
            Assert.False(ICommandDiscoverer.IsCommandType(typeof(int)));
            Assert.False(ICommandDiscoverer.IsCommandType(typeof(MissingAttribute)));
            Assert.False(ICommandDiscoverer.IsCommandType(typeof(MissingInheritance)));
            Assert.False(ICommandDiscoverer.IsCommandType(typeof(IInterfacesAreNotCommands)));
            Assert.False(ICommandDiscoverer.IsCommandType(typeof(NeitherAreAbstractClasses)));
            
            var validType = CommandGenerator.WithName("test").Generate().GetType();
            
            Assert.True(ICommandDiscoverer.IsCommandType(validType));
        }
    }
}