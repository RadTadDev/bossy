using System.Collections.Generic;
using Bossy.Frontend;

namespace Bossy.Execution
{
    public class CommandGroup
    {
        public IReadOnlyList<CommandGraphNode> Commands { get; }
        
        public IContentView View { get; }
        
        public CommandGroup(List<CommandGraphNode> commands, IContentView view)
        {
            Commands = commands;
            View = view;
        }
    }
}