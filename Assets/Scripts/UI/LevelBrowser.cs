using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelBrowser : MonoBehaviour
{
    [SerializeField] Transform content;      // parent des items
    [SerializeField] LevelListItem itemPrefab;
    [SerializeField] string levelEditorSceneName = "LevelEditor";
    
    [SerializeField] GameObject emptyState;      // <- ton texte "Aucun niveau"

    List<GameObject> _spawned = new();

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();

        var levels = LevelIO.GetAllLevels();
        
        // toggle message vide
        if (emptyState) emptyState.SetActive(levels.Count == 0);
        
        foreach (var li in levels)
        {
            var item = Instantiate(itemPrefab, content);
            item.Init(li, OnEditClicked, OnDeleteClicked);
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
        // confirmation rapide via ton PopupService
        PopupService.Instance?.Warn("Supprimer ce niveau ?",
            $"« {info.name} » sera définitivement supprimé.",
            0f);
        // Si tu veux un vrai bouton Oui/Non, on peut faire un Popup de confirmation.
        // Pour rester simple ici, on supprime directement :
        LevelIO.Delete(info.fullPath);
        Refresh();
    }

    // Bouton "Nouveau niveau"
    public void OnNewLevelClicked()
    {
        LevelBridge.Clear();
        SceneManager.LoadScene(levelEditorSceneName);
    }
}

