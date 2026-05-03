using Bossy;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class BossyStartup
{
    private static BossyConsole _bossy;
    static BossyStartup()
    {
        EditorApplication.delayCall += Start;
    }

    private static void Start()
    {
        EditorApplication.delayCall -= Start;
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .Build();
    }
}

#else

using UnityEngine;

public class BossyStartup
{
    private static BossyConsole _bossy;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Start()
    {
        _bossy = BossyBuilder
            .GetCommands()
            .Automatically()
            .Build();
    }
}
#endif  