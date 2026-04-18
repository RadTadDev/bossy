using System;
using Bossy.Editor.UI;

namespace Bossy.FrontEnd
{
    public static class UserInterfaceFactory
    {
        public static UserInterface Get(UserInterfaceType type)
        {
            return type switch
            {
#if UNITY_EDITOR

                UserInterfaceType.EDITOR_CommandLine => new CommandLineInterface(EditorCommandLineGUI.Create()),
                UserInterfaceType.EDITOR_Graphical => null,
#endif

                UserInterfaceType.CommandLine => null,
                UserInterfaceType.Graphical => null,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}