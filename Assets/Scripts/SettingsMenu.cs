using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI – Audio")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("UI – Vidéo")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    // liste unique par largeur/hauteur (sans doublons de fréquence)
    Resolution[] _resolutionsWH;
    bool _updating;

    const string PREF_RES = "video_res_index";
    const string PREF_FS  = "video_fullscreen";

    void Awake()
    {
        // Construit une liste unique par (width, height)
        _resolutionsWH = Screen.resolutions
            .Select(r => (r.width, r.height))
            .Distinct()
            .Select(t => new Resolution { width = t.width, height = t.height, refreshRate = Screen.currentResolution.refreshRate })
            .ToArray();
    }

    void Start()
    {
        // --- Vidéo : Résolution + Fullscreen ---
        if (resolutionDropdown)
        {
            resolutionDropdown.ClearOptions();
            var opts = _resolutionsWH.Select(r => $"{r.width} x {r.height}").ToList();
            resolutionDropdown.AddOptions(opts);

            int currentIndex = System.Array.FindIndex(_resolutionsWH, r => r.width == Screen.width && r.height == Screen.height);
            int savedIndex   = PlayerPrefs.GetInt(PREF_RES, -1);
            if (savedIndex >= 0 && savedIndex < _resolutionsWH.Length) currentIndex = savedIndex;

            resolutionDropdown.value = Mathf.Clamp(currentIndex, 0, _resolutionsWH.Length - 1);
            resolutionDropdown.RefreshShownValue();
        }

        if (fullscreenToggle)
        {
            bool savedFs = PlayerPrefs.GetInt(PREF_FS, Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = savedFs;
            fullscreenToggle.isOn = savedFs;
        }

        // --- Audio : synchronise les sliders avec AudioManager ---
        SyncAudioUI();
        

        // Brancher les events
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (fullscreenToggle)   fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (masterSlider)       masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider)        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider)          sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    void OnDestroy()
    {
        if (resolutionDropdown) resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
        if (fullscreenToggle)   fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        if (masterSlider)       masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicSlider)        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider)          sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }

    // ---------- AUDIO ----------
    void SyncAudioUI()
    {
        var am = AudioManager.Instance;
        if (!am) return;

        _updating = true;
        if (masterSlider) masterSlider.value = am.Master; // 0..1
        if (musicSlider)  musicSlider.value  = am.Music;  // 0..1
        if (sfxSlider)    sfxSlider.value    = am.Sfx;    // 0..1
        _updating = false;
    }

    public void SetMasterVolume(float v){ if (_updating) return; AudioManager.Instance?.SetMaster(v); }
    public void SetMusicVolume (float v){ if (_updating) return; AudioManager.Instance?.SetMusic (v); }
    public void SetSfxVolume   (float v){ if (_updating) return; AudioManager.Instance?.SetSfx   (v); }

    // ---------- VIDEO ----------
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FS, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        if (_resolutionsWH == null || index < 0 || index >= _resolutionsWH.Length) return;
        var res = _resolutionsWH[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(PREF_RES, index);
        PlayerPrefs.Save();
    }
    
    void OnEnable() => RefreshAudioUI(); // auto-sync à chaque ouverture

    public void RefreshAudioUI()
    {
        var am = AudioManager.Instance; if (!am) return;
        _updating = true;
        if (masterSlider) masterSlider.SetValueWithoutNotify(am.Master); // 0..1
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(am.Sfx);       // 0..1
        _updating = false;
    }

    
}
