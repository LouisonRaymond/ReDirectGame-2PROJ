using UnityEngine;
//using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private GameObject rulesWindow;
    [SerializeField] private GameObject makeALevelWindow;
    private bool _isSettingsOpen;
    private bool _isRulesOpen;
    private bool _isMakeALevelOpen;

    public void StartGame()
    {
        // Load the game scene (assuming it's named "GameScene")
        //UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();

        // If running in the editor, stop playing
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
        _isSettingsOpen = _isRulesOpen = _isMakeALevelOpen = false;

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

}
