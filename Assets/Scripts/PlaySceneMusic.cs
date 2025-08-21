// PlaySceneMusic.cs
using UnityEngine;
public class PlaySceneMusic : MonoBehaviour
{
    public AudioClip clip;
    public bool loop = true;
    void Start() { AudioManager.Instance?.PlayMusic(clip, loop); }
}