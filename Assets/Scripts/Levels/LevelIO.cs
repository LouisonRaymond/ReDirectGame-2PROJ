using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LevelIO
{
    public const string FolderName = "HandMainLevel";
    public const string Extension = ".json";

    public static string RootFolder {
        get {
            var p = Path.Combine(Application.persistentDataPath, FolderName);
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            return p;
        }
    }

    public struct LevelInfo {
        public string name;       
        public string fullPath;   
        public DateTime modified; 
        public long sizeBytes;
    }

    public static List<LevelInfo> GetAllLevels()
    {
        var list = new List<LevelInfo>();
        if (!Directory.Exists(RootFolder)) return list;

        foreach (var path in Directory.GetFiles(RootFolder, "*" + Extension))
        {
            var fi = new FileInfo(path);
            list.Add(new LevelInfo {
                name = Path.GetFileNameWithoutExtension(path),
                fullPath = path,
                modified = fi.LastWriteTime,
                sizeBytes = fi.Length
            });
        }
       
        list.Sort((a,b) => b.modified.CompareTo(a.modified));
        return list;
    }

    public static string MakeUniquePath(string baseName)
    {
        string safe = string.IsNullOrWhiteSpace(baseName) ? "Level" : baseName.Trim();
        string path = Path.Combine(RootFolder, safe + Extension);
        int i = 1;
        while (File.Exists(path)) {
            path = Path.Combine(RootFolder, $"{safe} ({i++}){Extension}");
        }
        return path;
    }

    public static void Save(LevelData data)
    {
        if (data == null) { Debug.LogError("LevelIO.Save: data null"); return; }
        if (string.IsNullOrWhiteSpace(data.levelName)) data.levelName = "Level";
        string path = MakeUniquePath(data.levelName);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"Level saved: {path}");
    }

    public static void SaveOverwrite(LevelData data, string fullPath)
    {
        if (data == null || string.IsNullOrEmpty(fullPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, JsonUtility.ToJson(data, true));
        Debug.Log($"Level overwritten: {fullPath}");
    }

    public static LevelData Load(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return null;
        var json = File.ReadAllText(fullPath);
        var data = JsonUtility.FromJson<LevelData>(json);
        if (data != null && string.IsNullOrWhiteSpace(data.levelName))
            data.levelName = Path.GetFileNameWithoutExtension(fullPath);
        return data;
    }

    public static void Delete(string fullPath)
    {
        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            File.Delete(fullPath);
    }
}

