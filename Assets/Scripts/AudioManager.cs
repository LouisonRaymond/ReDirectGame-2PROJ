// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    public AudioMixer mixer;                     // assigner ton AudioMixer
    public AudioMixerGroup musicGroup;           // route musique
    public AudioMixerGroup sfxGroup;             // route effets
    
    [Header("Gameplay Clips")]
    public AudioClip sfxHit;       // rebond / changement de direction
    public AudioClip sfxTeleport;  // tp entrée/sortie
    public AudioClip sfxGoal;      // endpoint
    public AudioClip sfxFail;      // sortie écran / échec

    // Noms des paramètres exposés dans le mixer (Exposed Parameters)
    const string PAR_MASTER = "SoundVolume";
    const string PAR_MUSIC  = "MusicVol";
    const string PAR_SFX    = "EffectsVol";

    // PlayerPrefs keys
    const string KEY_MASTER = "vol_master";
    const string KEY_MUSIC  = "vol_music";
    const string KEY_SFX    = "vol_sfx";

    [Header("Music/SFX sources")]
    public AudioSource musicSource;              // une source musique
    [Range(1,12)] public int sfxVoices = 6;      // pool SFX
    List<AudioSource> sfxPool;

    [Header("UI Clips")]
    public AudioClip sfxUiClick;
    public AudioClip sfxUiHover;

    // caches linéaires 0..1
    float _master = 0.8f, _music = 0.8f, _sfx = 1f;

    void Awake()
    {
        // Singleton + persistance
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Prépare sources
        if (!musicSource) {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.playOnAwake = false;
        }
        if (sfxPool == null) {
            sfxPool = new List<AudioSource>(sfxVoices);
            for (int i = 0; i < sfxVoices; i++) {
                var a = gameObject.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.outputAudioMixerGroup = sfxGroup;
                sfxPool.Add(a);
            }
        }

        // Charge et applique les volumes sauvegardés
        LoadVolumes();
        ApplyVolumesToMixer();
    }

    // --- volumes (0..1 linéaire stocké dans prefs) ---
    public float Master => _master;
    public float Music  => _music;
    public float Sfx    => _sfx;

    public void SetMaster(float v){ _master = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumesToMixer(); }
    public void SetMusic (float v){ _music  = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumesToMixer(); }
    public void SetSfx    (float v){ _sfx   = Mathf.Clamp01(v); SaveVolumes(); ApplyVolumesToMixer(); }

    void LoadVolumes()
    {
        _master = PlayerPrefs.GetFloat(KEY_MASTER, 0.8f);
        _music  = PlayerPrefs.GetFloat(KEY_MUSIC , 0.8f);
        _sfx    = PlayerPrefs.GetFloat(KEY_SFX   , 1.0f);
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, _master);
        PlayerPrefs.SetFloat(KEY_MUSIC , _music);
        PlayerPrefs.SetFloat(KEY_SFX   , _sfx);
        PlayerPrefs.Save();
    }

    void ApplyVolumesToMixer()
    {
        if (!mixer) return;
        mixer.SetFloat(PAR_MASTER, LinearToDb(_master));
        mixer.SetFloat(PAR_MUSIC , LinearToDb(_music));
        mixer.SetFloat(PAR_SFX   , LinearToDb(_sfx));
    }

    static float LinearToDb(float v)
    {
        // 0 → -80 dB, 1 → 0 dB
        return Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
    }

    // --- SFX helpers ---
    public void PlaySfx(AudioClip clip, float volume = 1f, float pitchMin = 0.98f, float pitchMax = 1.02f)
    {
        if (!clip || sfxPool == null || sfxPool.Count == 0) return;
        // cherche une voix libre
        foreach (var a in sfxPool)
        {
            if (!a.isPlaying)
            {
                a.pitch = Random.Range(pitchMin, pitchMax);
                a.PlayOneShot(clip, volume);
                return;
            }
        }
        // fallback: première voix
        var f = sfxPool[0];
        f.pitch = Random.Range(pitchMin, pitchMax);
        f.PlayOneShot(clip, volume);
    }
    
    // --- dans AudioManager ---
    public void PlayMusic(AudioClip clip, bool loop = true, float fade = 0.35f)
    {
        if (!musicSource) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        StartCoroutine(CoPlayMusic(clip, loop, fade));
    }

    System.Collections.IEnumerator CoPlayMusic(AudioClip clip, bool loop, float fade)
    {
        float start = musicSource.volume;
        // fade out
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t/fade);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        if (clip) musicSource.Play();
        // fade in
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, 1f, t/fade);
            yield return null;
        }
        musicSource.volume = 1f;
    }
    
    public void PlayHit(float volume = 1f)      => PlaySfx(sfxHit,      volume);
    public void PlayTeleport(float volume = 1f) => PlaySfx(sfxTeleport, volume);
    public void PlayGoal(float volume = 1f)     => PlaySfx(sfxGoal,     volume);
    public void PlayFail(float volume = 1f)     => PlaySfx(sfxFail,     volume);

    public void PlayUiClick(float volume = 1f) => PlaySfx(sfxUiClick, volume);
    public void PlayUiHover(float volume = 1f) => PlaySfx(sfxUiHover, volume);
}
