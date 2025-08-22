// Assets/Scripts/Levels/PlayBridge.cs
public static class PlayBridge
{
    public static int levelIndex = 0;
    public static string levelResourceName = null;
    
    // >>> NEW : lancer un niveau depuis un fichier JSON (éditeur / HandMainLevel)
    public static string diskPathToLoad;    // null = mode campagn
}
