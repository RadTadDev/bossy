using Bossy;
using Bossy.Frontend.Parsing;
using UnityEngine;

#if UNITY_EDITOR
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

#else
public class BossyStartup
{
    private static BossyConsole _bossy;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Start()
    {
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .WithAdapter<StringAdapter>()
            .Build();
    }
}
#endif  