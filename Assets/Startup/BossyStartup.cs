using Bossy;
using JetBrains.Annotations;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class BossyStartup
{
    [UsedImplicitly]
    private static BossyConsole _bossy;
    
    static BossyStartup()
    {
        EditorApplication.delayCall += Start;
    }

    private static void Start()
    {
        EditorApplication.delayCall -= Start;
        _bossy = CommonStartup.Make();
    }
}

#else

using UnityEngine;

public class BossyStartup
{
    [UsedImplicitly]
    private static BossyConsole _bossy;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Start()
    {
        _bossy = CommonStartup.Make();
    }
}
#endif  

public static class CommonStartup
{
    public static BossyConsole Make()
    {
        var binder = new MyBinder();
        
        binder.RegisterSingleton("Hello binding world!");
        binder.RegisterSingleton(42);
        
        return BossyBuilder
            .GetCommands()
            .Automatically()
            .WithBindings(binder)
            .Build();
    }
}