using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelBrowser : MonoBehaviour
{
    [SerializeField] Transform content;          // parent des items
    [SerializeField] LevelListItem itemPrefab;   // ton prefab d’item (avec bouton Play)
    [SerializeField] string levelEditorSceneName = "LevelEditor";
    [SerializeField] GameObject emptyState;      // texte "Aucun niveau"

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
            // ⬇️ on passe aussi le callback Play
            item.Init(li, OnEditClicked, OnDeleteClicked, OnPlayClicked);
            _spawned.Add(item.gameObject);
        }
    }

    void OnEditClicked(LevelIO.LevelInfo info)
    {
        LevelBridge.pathToLoad = info.fullPath;   // l’éditeur chargera ce fichier
        LevelBridge.currentPath = info.fullPath;  // pour SaveOverwrite
        SceneManager.LoadScene(levelEditorSceneName);
    }

    void OnDeleteClicked(LevelIO.LevelInfo info)
    {
        // Ici tu supprimes direct. Si tu veux une vraie confirmation, on peut brancher un Popup Oui/Non.
        LevelIO.Delete(info.fullPath);
        Refresh();
    }

    void OnPlayClicked(LevelIO.LevelInfo info)
    {
        // Lancer la PlayScene depuis un fichier disque (pas la campagne Resources)
        PlayBridge.diskPathToLoad = info.fullPath;
        PlayBridge.levelResourceName = null;
        PlayBridge.levelIndex = -1; // signale "hors campagne"
        SceneManager.LoadScene("PlayScene");
    }

    // Bouton "Nouveau niveau"
    public void OnNewLevelClicked()
    {
        LevelBridge.Clear();
        SceneManager.LoadScene(levelEditorSceneName);
    }
}