using UnityEngine;
//using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsWindow;
    public GameObject rulesWindow;
    private bool _isSettingsOpen;
    private bool _isRulesOpen;

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

    public void OpenSettings()
    {
        // Activate the settings menu
        _isSettingsOpen = !_isSettingsOpen;
        settingsWindow.SetActive(_isSettingsOpen);

        // If rules are open, close them
        if (_isRulesOpen)
        {
            rulesWindow.SetActive(false);
            _isRulesOpen = false;
        }
    }

    public void OpenRules()
    {
        _isRulesOpen = !_isRulesOpen;
        rulesWindow.SetActive(_isRulesOpen);
        
        // If settings are open, close them
        if (_isSettingsOpen) 
        {
            settingsWindow.SetActive(false);
            _isSettingsOpen = false;
        }
    }

}
