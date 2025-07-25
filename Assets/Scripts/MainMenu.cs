using UnityEngine;
//using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsWindow;
    private bool _isSettingsOpen;

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
    }

}
