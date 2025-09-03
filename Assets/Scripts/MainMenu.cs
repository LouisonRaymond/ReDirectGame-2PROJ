using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private GameObject rulesWindow;
    [SerializeField] private GameObject makeALevelWindow;
    [SerializeField] private GameObject playWindow;
    private bool _isSettingsOpen;
    private bool _isRulesOpen;
    private bool _isMakeALevelOpen;
    private bool _isPlayOpen;

    public void StartGame()
    {
        //UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void ShowOnly(GameObject panelToToggle, ref bool stateFlag)
    {
        bool willOpen = !stateFlag;

        settingsWindow.SetActive(false);
        rulesWindow.SetActive(false);
        makeALevelWindow.SetActive(false);
        playWindow.SetActive(false);
        _isSettingsOpen = _isRulesOpen = _isMakeALevelOpen = _isPlayOpen = false;

        if (willOpen)
        {
            panelToToggle.SetActive(true);
            stateFlag = true;

            if (panelToToggle == settingsWindow)
                settingsWindow.GetComponentInChildren<SettingsMenu>(true)?.RefreshAudioUI();

            if (panelToToggle == makeALevelWindow)
                makeALevelWindow.GetComponentInChildren<LevelBrowser>(true)?.Refresh();

            
        }

        // AudioManager.Instance?.PlayUiClick();
    }

    public void OpenSettings()   => ShowOnly(settingsWindow,   ref _isSettingsOpen);
    public void OpenRules()      => ShowOnly(rulesWindow,      ref _isRulesOpen);
    public void OpenMakeALevel() => ShowOnly(makeALevelWindow, ref _isMakeALevelOpen);
    public void OpenPlay()       => ShowOnly(playWindow,       ref _isPlayOpen);
}