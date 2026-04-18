using Bossy;
using UnityEditor;

[InitializeOnLoad]
public static class BossyStartup
{
    static BossyStartup()
    {
        BossyConsole.Create().Build();
    }
}