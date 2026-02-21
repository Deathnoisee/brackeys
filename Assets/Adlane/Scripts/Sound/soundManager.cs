using System;
using UnityEngine;

public class soundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundEffects;
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float OverAllVolume = 1f;

    public static soundManager Instance { get; private set; }
    

    private void Start()
    {
        playMusic("MenuMusic", 0.5f, 1f);
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlaySoundEffect(string name, float volume = 1f, float pitch = 1f, int sourceIndex = 0)
    {
        AudioClip clip = Array.Find(soundEffects, s => s.name == name);
        if (clip != null)
        {
            audioSources[sourceIndex].PlayOneShot(clip, volume*OverAllVolume);
            audioSources[sourceIndex].pitch = pitch;
        }
        else
        {
            Debug.LogWarning("Sound effect not found: " + name);
        }
    }
    //Play music, depends on the situation
    public void playMusic(string name, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = Array.Find(musicTracks, s => s.name == name);
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = volume*OverAllVolume;
            musicSource.pitch = pitch;
            LoopWithDifferentPitch();
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music track not found: " + name);
        }
    }
    //Stop music, depends on the situation
    public void StopMusic()
    {
        musicSource.Stop();
    }
    // called in pause menu
    public void PauseAll()
    {
        musicSource.Pause();
        foreach (var source in audioSources)
        {
            source.Pause();
        }
    }
    // called after the player unpauses the game
    public void ResumeAll()
    {
        musicSource.UnPause();
        foreach (var source in audioSources)
        {
            source.UnPause();
        }
    }
    private void LoopWithDifferentPitch()
    {
        musicSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
    }
    public void Update()
    {
        if (!musicSource.isPlaying)
        {
            LoopWithDifferentPitch();
            musicSource.Play();
        }
    }
    public void changeVolume(float volume)
    {
        OverAllVolume = volume;
        musicSource.volume = musicSource.volume * OverAllVolume;
        foreach (var source in audioSources)
        {
            source.volume = source.volume * OverAllVolume;
        }
    }
}
