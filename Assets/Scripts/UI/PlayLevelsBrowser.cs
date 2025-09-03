using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayLevelsBrowser : MonoBehaviour
{
    [SerializeField] Transform content;          
    [SerializeField] PlayLevelItem itemPrefab;
    [SerializeField] string playSceneName = "PlayScene";
    [SerializeField] GameObject emptyState;

    const string RES_FOLDER = "PlayLevels";      
    const string PROG_KEY = "play_progress";     

    TextAsset[] _levels; 

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        
        _levels = Resources.LoadAll<TextAsset>(RES_FOLDER);
        
        System.Array.Sort(_levels, (a,b) => ExtractNum(a.name).CompareTo(ExtractNum(b.name)));

        foreach (Transform c in content) Destroy(c.gameObject);

        if (emptyState) emptyState.SetActive(_levels.Length == 0);
        int progress = PlayerPrefs.GetInt(PROG_KEY, -1); 

        for (int i = 0; i < _levels.Length; i++)
        {
            var it = Instantiate(itemPrefab, content);
            bool locked = i > progress + 1;
            it.Init(i, locked, OnLevelClicked);
        }
    }

    int ExtractNum(string s)
    {
        
        int n = 0; string digits = "";
        foreach (char c in s) if (char.IsDigit(c)) digits += c;
        return int.TryParse(digits, out n) ? n : 9999;
    }

    void OnLevelClicked(int index)
    {
        
        PlayBridge.levelIndex = index;
        PlayBridge.levelResourceName = _levels[index].name; 
        SceneManager.LoadScene(playSceneName);
    }

    
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PROG_KEY);
        Refresh();
    }
}

