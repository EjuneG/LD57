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

    [Header("Sound Collections")]
    [SerializeField] private List<Sound> bgmSounds = new List<Sound>();
    [SerializeField] private List<Sound> sfxSounds = new List<Sound>();

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
        if (index < 0 || index >= bgmSounds.Count || bgmSounds[index].clip == null)
        {
            Debug.LogWarning("Invalid BGM index or clip is null");
            return;
        }

        // If the same BGM is requested, do nothing
        if (index == currentBgmIndex && bgmSource.isPlaying)
            return;

        Sound sound = bgmSounds[index];
        currentBgmIndex = index;
        
        bgmSource.clip = sound.clip;
        bgmSource.pitch = sound.pitch;
        bgmSource.volume = sound.volume * bgmVolume * masterVolume;
        bgmSource.Play();
    }

    // Play a BGM by name
    public void PlayBGM(string soundName)
    {
        for (int i = 0; i < bgmSounds.Count; i++)
        {
            if (bgmSounds[i].name == soundName)
            {
                PlayBGM(i);
                return;
            }
        }
        
        Debug.LogWarning($"BGM sound with name '{soundName}' not found");
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

    // Set BGM pitch for currently playing track
    public void SetBGMPitch(float pitch)
    {
        pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        bgmSource.pitch = pitch;
        
        // Update the pitch value in the sound object too
        if (currentBgmIndex >= 0 && currentBgmIndex < bgmSounds.Count)
        {
            bgmSounds[currentBgmIndex].pitch = pitch;
        }
    }
    
    // Set individual BGM volume
    public void SetBGMVolumeForSound(int index, float volume)
    {
        if (index < 0 || index >= bgmSounds.Count)
        {
            Debug.LogWarning("Invalid BGM index");
            return;
        }
        
        bgmSounds[index].volume = Mathf.Clamp01(volume);
        
        // If this is currently playing, update the source
        if (index == currentBgmIndex)
        {
            bgmSource.volume = bgmSounds[index].volume * bgmVolume * masterVolume;
        }
    }
    
    // Set individual BGM pitch
    public void SetBGMPitchForSound(int index, float pitch)
    {
        if (index < 0 || index >= bgmSounds.Count)
        {
            Debug.LogWarning("Invalid BGM index");
            return;
        }
        
        bgmSounds[index].pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        
        // If this is currently playing, update the source
        if (index == currentBgmIndex)
        {
            bgmSource.pitch = bgmSounds[index].pitch;
        }
    }

    // Set BGM volume (0-1)
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        
        // If currently playing, update with the sound's individual volume too
        if (currentBgmIndex >= 0 && currentBgmIndex < bgmSounds.Count)
        {
            bgmSource.volume = bgmSounds[currentBgmIndex].volume * bgmVolume * masterVolume;
        }
    }

    #endregion

    #region SFX Methods

    // Play a SFX by index
    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxSounds.Count || sfxSounds[index].clip == null)
        {
            Debug.LogWarning("Invalid SFX index or clip is null");
            return;
        }

        Sound sound = sfxSounds[index];
        
        // Store original pitch
        float originalPitch = sfxSource.pitch;
        
        // Apply settings and play
        sfxSource.pitch = sound.pitch;
        sfxSource.PlayOneShot(sound.clip, sound.volume);
        
        // Reset pitch
        sfxSource.pitch = originalPitch;
    }

    // Play a SFX by name
    public void PlaySFX(string soundName)
    {
        for (int i = 0; i < sfxSounds.Count; i++)
        {
            if (sfxSounds[i].name == soundName)
            {
                PlaySFX(i);
                return;
            }
        }
        
        Debug.LogWarning($"SFX sound with name '{soundName}' not found");
    }

    // Play a SFX with custom volume and pitch (temporary override)
    public void PlaySFXWithSettings(int index, float volumeScale = 1f, float pitch = 1f)
    {
        if (index < 0 || index >= sfxSounds.Count || sfxSounds[index].clip == null)
        {
            Debug.LogWarning("Invalid SFX index or clip is null");
            return;
        }

        Sound sound = sfxSounds[index];
        
        // Store original settings
        float originalPitch = sfxSource.pitch;
        
        // Apply new settings
        sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        
        // Play with adjusted volume (applying the scale to the sound's base volume)
        sfxSource.PlayOneShot(sound.clip, sound.volume * volumeScale);
        
        // Reset pitch (volume will be handled by PlayOneShot)
        sfxSource.pitch = originalPitch;
    }
    
    // Set individual SFX volume
    public void SetSFXVolumeForSound(int index, float volume)
    {
        if (index < 0 || index >= sfxSounds.Count)
        {
            Debug.LogWarning("Invalid SFX index");
            return;
        }
        
        sfxSounds[index].volume = Mathf.Clamp01(volume);
    }
    
    // Set individual SFX pitch
    public void SetSFXPitchForSound(int index, float pitch)
    {
        if (index < 0 || index >= sfxSounds.Count)
        {
            Debug.LogWarning("Invalid SFX index");
            return;
        }
        
        sfxSounds[index].pitch = Mathf.Clamp(pitch, 0.5f, 3f);
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
        
        // If BGM is playing, update with the sound's volume too
        if (currentBgmIndex >= 0 && currentBgmIndex < bgmSounds.Count)
        {
            bgmSource.volume = bgmSounds[currentBgmIndex].volume * bgmVolume * masterVolume;
        }
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