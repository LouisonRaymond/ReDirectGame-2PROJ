using UnityEngine;

public static class LevelBridge
{
    
    public static string pathToLoad = null;

    
    public static string currentPath = null;

    public static void Clear()
    {
        pathToLoad = null;
        currentPath = null;
    }
}
