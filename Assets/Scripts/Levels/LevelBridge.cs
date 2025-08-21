using UnityEngine;

public static class LevelBridge
{
    // Si non vide: l'éditeur doit charger ce fichier au Start
    public static string pathToLoad = null;

    // Si non vide: l'éditeur sauvegardera en écrasant ce fichier
    public static string currentPath = null;

    public static void Clear()
    {
        pathToLoad = null;
        currentPath = null;
    }
}
