using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelBrowser : MonoBehaviour
{
    [SerializeField] Transform content;          
    [SerializeField] LevelListItem itemPrefab;   
    [SerializeField] string levelEditorSceneName = "LevelEditor";
    [SerializeField] GameObject emptyState;      

    List<GameObject> _spawned = new();

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();

        var levels = LevelIO.GetAllLevels();

        if (emptyState) emptyState.SetActive(levels.Count == 0);

        foreach (var li in levels)
        {
            var item = Instantiate(itemPrefab, content);
            
            item.Init(li, OnEditClicked, OnDeleteClicked, OnPlayClicked);
            _spawned.Add(item.gameObject);
        }
    }

    void OnEditClicked(LevelIO.LevelInfo info)
    {
        LevelBridge.pathToLoad = info.fullPath;   
        LevelBridge.currentPath = info.fullPath;  
        SceneManager.LoadScene(levelEditorSceneName);
    }

    void OnDeleteClicked(LevelIO.LevelInfo info)
    {
        
        LevelIO.Delete(info.fullPath);
        Refresh();
    }

    void OnPlayClicked(LevelIO.LevelInfo info)
    {
        
        PlayBridge.diskPathToLoad = info.fullPath;
        PlayBridge.levelResourceName = null;
        PlayBridge.levelIndex = -1; 
        SceneManager.LoadScene("PlayScene");
    }

    
    public void OnNewLevelClicked()
    {
        LevelBridge.Clear();
        SceneManager.LoadScene(levelEditorSceneName);
    }
}