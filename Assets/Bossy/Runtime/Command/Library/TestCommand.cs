using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Runtime.Command.Library
{
    [Command("test", "A test command")]
    public class TestCommand : ICommand
    {
        [Switch('f', "A pretend flag.")] 
        private bool _flag;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            ctx.Write($"Flag is {_flag}");
            ctx.Write(await ctx.ReadAsync<float>());
            
            ctx.Write(await ctx.ReadAsync<bool>());

            ctx.Write(await ctx.ReadAsync<int>());
            
            return CommandStatus.Ok;
        }
    }
}