using Bossy.Command;
using Bossy.Frontend.Autocomplete;

namespace Bossy.Runtime.Command.Library
{
    [Command("test", "Used for testing.")]
    public class TestCommand : SimpleCommand
    {
        [Suggest(nameof(Suggest))]
        [Positional(0, "My test positional")] 
        private string _word;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write(_word);
            return CommandStatus.Ok;
        }

        private static string[] Suggest()
        {
            return new[] { "Hello", "beautiful", "world" };
        }
    }
}