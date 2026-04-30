using Bossy;
using Bossy.Frontend.Parsing;
using UnityEditor;

[InitializeOnLoad]
public static class BossyStartup
{
    private static BossyConsole _bossy;
    static BossyStartup()
    {
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .WithAdapter<StringAdapter>()
            .Build();
    }
}