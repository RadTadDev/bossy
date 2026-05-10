using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
    [Command("test", "Used for testing.")]
    public class TestCommand : SimpleCommand
    {
        [Switch('a', "First bool switch")]
        private bool _aBoolean;
        
        [Switch('b', "Second bool switch")]
        private bool _bBoolean;
        
        [Switch('c', "Third bool switch")]
        private bool _cBoolean;
        
        [Switch('s', "My test switch")] 
        private Vector2 _switchValue;
        
        [Suggest(nameof(Suggest))]
        [Positional(0, "My test positional")] 
        private Vector2 _firstPos;
        
        [Suggest(nameof(Another))]
        [Positional(1, "My other test positional")] 
        private string _secondPos;

        [Suggest(nameof(Optional))]
        [Optional(0, "My test optional")] 
        private float _optional;
        
        [Variadic("My test variadic")]
        private bool[] _variadic;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("The command ran successfully.");
            return CommandStatus.Ok;
        }

        private static string[] Suggest()
        {
            return new[] { "1 2", "10 4", "3 5" };
        }

        private static string[] Another()
        {
            return new[] { "Big", "Small" };
        }
        
        private static string[] Optional()
        {
            return new[] { "4.2", "2" };
        }
    }
    
    [Command("sub", "Subcommand of the test command.", typeof(TestCommand))]
    public class TestSubcommand : SimpleCommand
    {
        [Positional(0, "First sub positional")]
        private Vector3 _subPos1;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("Executed the subcommand correctly");
            return CommandStatus.Ok;
        }
    }
    
    [Command("otherSub", "Other subcommand of the test command.", typeof(TestCommand))]
    public class OtherTestSubcommand : SimpleCommand
    {
        [Switch('w', "Test switch")]
        private string _word;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("Executed the subcommand correctly");
            return CommandStatus.Ok;
        }
    }
}