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
        private string _firstPos;
        
        [Suggest(nameof(Suggest))]
        [Positional(1, "My other test positional")] 
        private string _secondPos;

        [Optional(0, "My test optional")] 
        private string _optional;
        
        [Variadic("My test variadic")]
        private string[] _variadic;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("The command ran successfully.");
            return CommandStatus.Ok;
        }

        private static string[] Suggest()
        {
            return new[] { "Hello", "beautiful", "world" };
        }
    }
}