// Assets/Scripts/UI/PlayLevelsBrowser.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayLevelsBrowser : MonoBehaviour
{
    [SerializeField] Transform content;          // parent des items
    [SerializeField] PlayLevelItem itemPrefab;
    [SerializeField] string playSceneName = "PlayScene";
    [SerializeField] GameObject emptyState;

    const string RES_FOLDER = "PlayLevels";      // => Resources/PlayLevels
    const string PROG_KEY = "play_progress";     // 0-based : plus haut niveau fini

    TextAsset[] _levels; // ordonnés

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        // charge tous les JSON depuis Resources/PlayLevels
        _levels = Resources.LoadAll<TextAsset>(RES_FOLDER);
        // trie par numéro dans le nom "LevelX"
        System.Array.Sort(_levels, (a,b) => ExtractNum(a.name).CompareTo(ExtractNum(b.name)));

        foreach (Transform c in content) Destroy(c.gameObject);

        if (emptyState) emptyState.SetActive(_levels.Length == 0);
        int progress = PlayerPrefs.GetInt(PROG_KEY, -1); // -1 : rien fini ; niveau 0 déverrouillé

        for (int i = 0; i < _levels.Length; i++)
        {
            var it = Instantiate(itemPrefab, content);
            bool locked = i > progress + 1;
            it.Init(i, locked, OnLevelClicked);
        }
    }

    int ExtractNum(string s)
    {
        // "Level12" → 12 ; sinon 9999
        int n = 0; string digits = "";
        foreach (char c in s) if (char.IsDigit(c)) digits += c;
        return int.TryParse(digits, out n) ? n : 9999;
    }

    void OnLevelClicked(int index)
    {
        // passe l'index + le nom ressource au Play
        PlayBridge.levelIndex = index;
        PlayBridge.levelResourceName = _levels[index].name; // sans extension
        SceneManager.LoadScene(playSceneName);
    }

    // expose pour reset progression (optionnel)
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PROG_KEY);
        Refresh();
    }
}

