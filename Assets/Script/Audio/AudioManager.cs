using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private List<AudioClip> sfxClips;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // Current BGM index for tracking
    private int currentBgmIndex = -1;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize audio sources if not set in the inspector
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();
        
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        // Configure BGM source
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        
        // Configure SFX source
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // Apply initial volume settings
        ApplyVolumeSettings();
    }

    // Apply all volume settings
    private void ApplyVolumeSettings()
    {
        bgmSource.volume = bgmVolume * masterVolume;
        sfxSource.volume = sfxVolume * masterVolume;
    }

    #region BGM Methods

    // Play a BGM by index
    public void PlayBGM(int index)
    {
        if (index < 0 || index >= bgmClips.Count || bgmClips[index] == null)
        {
            Debug.LogWarning("Invalid BGM index or clip is null");
            return;
        }

        // If the same BGM is requested, do nothing
        if (index == currentBgmIndex && bgmSource.isPlaying)
            return;

        currentBgmIndex = index;
        bgmSource.clip = bgmClips[index];
        bgmSource.Play();
    }

    // Play a BGM by name
    public void PlayBGM(string clipName)
    {
        for (int i = 0; i < bgmClips.Count; i++)
        {
            if (bgmClips[i] != null && bgmClips[i].name == clipName)
            {
                PlayBGM(i);
                return;
            }
        }
        
        Debug.LogWarning($"BGM clip with name '{clipName}' not found");
    }

    // Pause current BGM
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Pause();
    }

    // Resume paused BGM
    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    // Stop current BGM
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // Set BGM pitch
    public void SetBGMPitch(float pitch)
    {
        bgmSource.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
    }

    // Set BGM volume (0-1)
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    #endregion

    #region SFX Methods

    // Play a SFX by index
    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxClips.Count || sfxClips[index] == null)
        {
            Debug.LogWarning("Invalid SFX index or clip is null");
            return;
        }

        sfxSource.PlayOneShot(sfxClips[index]);
    }

    // Play a SFX by name
    public void PlaySFX(string clipName)
    {
        for (int i = 0; i < sfxClips.Count; i++)
        {
            if (sfxClips[i] != null && sfxClips[i].name == clipName)
            {
                PlaySFX(i);
                return;
            }
        }
        
        Debug.LogWarning($"SFX clip with name '{clipName}' not found");
    }

    // Play a SFX with custom volume and pitch
    public void PlaySFXWithSettings(int index, float volumeScale = 1f, float pitch = 1f)
    {
        if (index < 0 || index >= sfxClips.Count || sfxClips[index] == null)
        {
            Debug.LogWarning("Invalid SFX index or clip is null");
            return;
        }

        // Store original settings
        float originalPitch = sfxSource.pitch;
        
        // Apply new settings
        sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        
        // Play with adjusted volume
        sfxSource.PlayOneShot(sfxClips[index], volumeScale);
        
        // Reset pitch (volume will be handled by PlayOneShot)
        sfxSource.pitch = originalPitch;
    }

    // Set SFX volume (0-1)
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    #endregion

    #region Master Volume Control

    // Set master volume (0-1)
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    // Get current master volume
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    // Get current BGM volume
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    
    // Get current SFX volume
    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    #endregion
}