using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NarrationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private GameObject textBox;
    
    [Header("Typewriter Settings")]
    [SerializeField] private float baseTypeSpeed = 0.05f;
    [SerializeField] private float speedVariation = 0.03f;
    [SerializeField] private float pauseOnPunctuation = 0.2f;
    
    [Header("Auto-Advance Settings")]
    [SerializeField] private float waitTimeAfterLine = 2.0f;
    [SerializeField] private bool autoAdvance = true;
    
    [Header("Talking Sound Settings")]
    [SerializeField] private AudioClip normalTalkingSound;
    [SerializeField] private AudioClip evilTalkingSound;
    [SerializeField] private float talkingSoundFrequency = 0.08f; // How often to play normal sound effects
    [Range(0f, 1f)]
    [SerializeField] private float talkingSoundVolume = 0.3f;
    [Range(0.5f, 1.5f)]
    [SerializeField] private float talkingSoundPitch = 1f;
    [SerializeField] private float talkingSoundPitchVariation = 0.1f;
    
    [Header("Doctor Talking Settings")]
    [Tooltip("Enable this to use looping talk sounds for doctor mode")]
    [SerializeField] private bool useLoopingTalkSound = true;
    [SerializeField] private bool applyPitchVariationToLoop = false;
    
    [Header("Level Transition")]
    [SerializeField] private bool enableLevelTransitions = true;
    
    // Event that fires when a narration set completes
    public event Action<NarrationSet> OnNarrationSetCompleted;
    // Event that fires when the last line of a narration finishes
    public event Action<NarrationLine> OnNarrationLineCompleted;
    
    private NarrationSet currentSet;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool canAdvance = false;
    private Coroutine typingCoroutine;
    private bool isPartOfSet = false; // Flag to track if current line is part of a set
    private NarrationLine currentLine; // Track the current line being displayed
    
    private AudioSource audioSource; // For voice over clips
    private AudioSource talkingSoundSource; // For normal typing sound effects
    private AudioSource loopingSoundSource; // For looping voice during dialog
    
    private void Awake()
    {
        // Set up audio sources
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 1)
        {
            audioSource = sources[0];
        }
        else
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Add a separate audio source for talking sounds
        if (sources.Length >= 2)
        {
            talkingSoundSource = sources[1];
        }
        else
        {
            talkingSoundSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Add a separate audio source for looping talking sound
        if (sources.Length >= 3)
        {
            loopingSoundSource = sources[2];
        }
        else
        {
            loopingSoundSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure talking sound source
        talkingSoundSource.loop = false;
        talkingSoundSource.playOnAwake = false;
        talkingSoundSource.volume = talkingSoundVolume;
        
        // Configure looping sound source
        loopingSoundSource.loop = true;
        loopingSoundSource.playOnAwake = false;
        loopingSoundSource.volume = talkingSoundVolume;
        
        // Hide text box initially
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Check for click/tap to advance
        if (Input.GetMouseButtonDown(0) && canAdvance)
        {
            AdvanceNarration();
        }
    }
    
    /// <summary>
    /// Play a single narration line
    /// </summary>
    public void PlayLine(NarrationLine line)
    {
        if (line == null) return;
        
        // Show text box
        if (textBox != null)
        {
            textBox.SetActive(true);
        }
        
        // Store the current line
        currentLine = line;
        
        // Stop any existing narration and reset state
        StopAllCoroutines();
        isTyping = false;
        
        // Make sure any looping sound is stopped when starting a new line
        StopLoopingTalkSound();
        
        // If a narration was in progress, consider it interrupted
        if (currentSet != null && isPartOfSet)
        {
            // Notify that the set was interrupted
            var interruptedSet = currentSet;
            Debug.Log("Narration set interrupted");
            
            // Reset set-related state
            isPartOfSet = false;
        }
        
        // Always create a fresh set for this line
        currentLineIndex = 0;
        currentSet = ScriptableObject.CreateInstance<NarrationSet>();
        currentSet.narrationLines = new List<NarrationLine> { line };
        
        // Start typing
        textDisplay.color = line.textcolor;
        typingCoroutine = StartCoroutine(TypeText(line.text, line.voiceType));
        
        // Play voice over if available
        if (line.voiceOver != null && line.voiceOver.clip != null && audioSource != null)
        {
            audioSource.clip = line.voiceOver.clip;
            audioSource.volume = line.voiceOver.volume;
            audioSource.pitch = line.voiceOver.pitch;
            audioSource.Play();
        }
    }
    
    /// <summary>
    /// Start playing a narration set
    /// </summary>
    public void PlayNarrationSet(NarrationSet set)
    {
        if (set == null || set.narrationLines == null || set.narrationLines.Count == 0) return;
        
        // If a narration was in progress, consider it interrupted
        if (currentSet != null && currentSet != set)
        {
            // Notify that the set was interrupted
            var interruptedSet = currentSet;
            Debug.Log("Narration set interrupted by another set");
        }
        
        // Store the new set
        currentSet = set;
        currentLineIndex = 0;
        isPartOfSet = true; // Set flag to indicate we're playing a set
        
        // Play the first line, but keep isPartOfSet flag
        bool wasPartOfSet = isPartOfSet;
        if (set.narrationLines.Count > 0)
        {
            currentLine = set.narrationLines[currentLineIndex];
            
            // Show text box
            if (textBox != null)
            {
                textBox.SetActive(true);
            }
            
            // Stop any existing narration and reset state
            StopAllCoroutines();
            isTyping = false;
            
            // Make sure any looping sound is stopped when starting a new line
            StopLoopingTalkSound();
            
            // Start typing
            textDisplay.color = currentLine.textcolor;
            typingCoroutine = StartCoroutine(TypeText(currentLine.text, currentLine.voiceType));
            
            // Play voice over if available
            if (currentLine.voiceOver != null && currentLine.voiceOver.clip != null && audioSource != null)
            {
                audioSource.clip = currentLine.voiceOver.clip;
                audioSource.volume = currentLine.voiceOver.volume;
                audioSource.pitch = currentLine.voiceOver.pitch;
                audioSource.Play();
            }
            
            // Restore the isPartOfSet flag
            isPartOfSet = wasPartOfSet;
        }
    }
    
    /// <summary>
    /// Advance to the next line in the current narration set
    /// </summary>
    public void AdvanceNarration()
    {
        // If currently typing, show full text immediately
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            
            // Make sure to stop any looping sound when skipping text
            if (useLoopingTalkSound)
            {
                StopLoopingTalkSound();
            }
            
            if (textDisplay != null && currentSet != null && currentLineIndex < currentSet.narrationLines.Count)
            {
                textDisplay.text = currentSet.narrationLines[currentLineIndex].text;
            }
            isTyping = false;
            canAdvance = true;
            
            // Check if the current line has a level transition and trigger it
            if (currentLine != null && currentLine.transitionAfterLine && !string.IsNullOrEmpty(currentLine.transitionToLevel))
            {
                CheckAndTriggerTransition(currentLine);
                return; // Don't advance if transitioning
            }
            return;
        }
        
        // Check if we have a current set and if there are more lines
        if (currentSet == null || currentLineIndex >= currentSet.narrationLines.Count - 1)
        {
            // End of narration set
            EndNarration();
            return;
        }
        
        // Advance to next line
        currentLineIndex++;
        Debug.Log($"Advancing to line {currentLineIndex} of {currentSet.narrationLines.Count}");
        
        // Get the next line
        currentLine = currentSet.narrationLines[currentLineIndex];
        
        // Show text box if not already visible
        if (textBox != null && !textBox.activeSelf)
        {
            textBox.SetActive(true);
        }
        
        // Stop any existing narration and reset state
        StopAllCoroutines();
        isTyping = false;
        
        // Make sure any looping sound is stopped when starting a new line
        StopLoopingTalkSound();
        
        // Start typing the new line
        textDisplay.color = currentLine.textcolor;
        typingCoroutine = StartCoroutine(TypeText(currentLine.text, currentLine.voiceType));
        
        // Play voice over if available
        if (currentLine.voiceOver != null && currentLine.voiceOver.clip != null && audioSource != null)
        {
            audioSource.clip = currentLine.voiceOver.clip;
            audioSource.volume = currentLine.voiceOver.volume;
            audioSource.pitch = currentLine.voiceOver.pitch;
            audioSource.Play();
        }
    }
    
    /// <summary>
    /// End the narration and hide the text box
    /// </summary>
    public void EndNarration()
    {
        StopAllCoroutines();
        
        // Stop looping talking sound
        StopLoopingTalkSound();
        
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
        
        // Trigger completion event if there's a current set
        if (currentSet != null)
        {
            OnNarrationSetCompleted?.Invoke(currentSet);
        }
        
        // Get the last narration line before clearing the set
        var lastLine = currentLine ?? (currentSet != null && currentLineIndex < currentSet.narrationLines.Count 
            ? currentSet.narrationLines[currentLineIndex] 
            : null);
            
        // Check for transition in the last line
        if (lastLine != null)
        {
            CheckAndTriggerTransition(lastLine);
        }
        
        // Check for set-level transition
        if (currentSet != null && currentSet.transitionAfterSet && !string.IsNullOrEmpty(currentSet.transitionToLevel))
        {
            // Trigger the level transition
            GameEvents.TriggerOnLevelTransition(currentSet.transitionToLevel);
        }
        
        // Reset state
        currentSet = null;
        currentLine = null;
        isTyping = false;
        canAdvance = false;
        isPartOfSet = false; // Reset the flag
        
        // Trigger the line completion event
        if (lastLine != null)
        {
            OnNarrationLineCompleted?.Invoke(lastLine);
        }
    }
    
    /// <summary>
    /// Check if a narration line has a transition and trigger it if necessary
    /// </summary>
    private void CheckAndTriggerTransition(NarrationLine line)
    {
        if (line != null && line.transitionAfterLine && !string.IsNullOrEmpty(line.transitionToLevel) && enableLevelTransitions)
        {
            Debug.Log($"Triggering transition to level: {line.transitionToLevel}");
            GameEvents.TriggerOnLevelTransition(line.transitionToLevel);
        }
    }
    
    /// <summary>
    /// Play a typing sound based on voice type (non-looping version)
    /// </summary>
    private void PlayTalkingSound(NarrationVoiceType voiceType)
    {
        if (talkingSoundSource == null) return;
        
        // If we're using looping sound, don't play individual sounds
        if (useLoopingTalkSound && loopingSoundSource != null && loopingSoundSource.isPlaying)
        {
            return;
        }
        
        // Select the appropriate sound clip
        AudioClip soundToPlay = voiceType == NarrationVoiceType.Evil ? evilTalkingSound : normalTalkingSound;
        
        // Don't play if we don't have a clip
        if (soundToPlay == null) return;
        
        // Add some pitch variation to make it sound more natural
        float randomPitch = talkingSoundPitch + UnityEngine.Random.Range(-talkingSoundPitchVariation, talkingSoundPitchVariation);
        talkingSoundSource.pitch = randomPitch;
        
        // Play the sound
        talkingSoundSource.PlayOneShot(soundToPlay, talkingSoundVolume);
    }
    
    /// <summary>
    /// Start the looping talking sound based on the voice type
    /// </summary>
    private void StartLoopingTalkSound(NarrationVoiceType voiceType)
    {
        if (!useLoopingTalkSound || loopingSoundSource == null) return;
        
        // Select the appropriate sound clip
        AudioClip soundToPlay = voiceType == NarrationVoiceType.Evil ? evilTalkingSound : normalTalkingSound;
        
        // Don't play if we don't have a clip
        if (soundToPlay == null) return;
        
        // Set up and start the looping talking sound
        loopingSoundSource.clip = soundToPlay;
        loopingSoundSource.volume = talkingSoundVolume;
        
        // Apply pitch - either fixed or with variation depending on settings
        if (applyPitchVariationToLoop)
        {
            float randomPitch = talkingSoundPitch + UnityEngine.Random.Range(-talkingSoundPitchVariation * 0.5f, talkingSoundPitchVariation * 0.5f);
            loopingSoundSource.pitch = randomPitch;
        }
        else
        {
            loopingSoundSource.pitch = talkingSoundPitch;
        }
        
        loopingSoundSource.Play();
    }
    
    /// <summary>
    /// Stop the looping talking sound
    /// </summary>
    private void StopLoopingTalkSound()
    {
        if (loopingSoundSource == null) return;
        
        if (loopingSoundSource.isPlaying)
        {
            loopingSoundSource.Stop();
        }
    }
    
    /// <summary>
    /// Typewriter effect coroutine with variable typing speed and talking sounds
    /// </summary>
    private IEnumerator TypeText(string text, NarrationVoiceType voiceType)
    {
        isTyping = true;
        canAdvance = false;
        
        // Start looping sound if enabled
        if (useLoopingTalkSound)
        {
            StartLoopingTalkSound(voiceType);
        }
        
        if (textDisplay != null)
        {
            textDisplay.text = "";
            
            float timeSinceLastSound = 0f;
            
            for (int i = 0; i < text.Length; i++)
            {
                // Add one character at a time
                textDisplay.text += text[i];
                
                // Creepy variable timing for typewriter effect
                float delay = baseTypeSpeed;
                
                // Add variation to make it feel more organic/creepy
                delay += UnityEngine.Random.Range(-speedVariation, speedVariation);
                
                // Longer pauses on punctuation
                bool isPunctuation = text[i] == '.' || text[i] == ',' || text[i] == '?' || text[i] == '!';
                if (isPunctuation)
                {
                    delay += pauseOnPunctuation;
                }
                
                // Only play individual talking sounds if not using looping sound
                if (!useLoopingTalkSound && !isPunctuation && timeSinceLastSound >= talkingSoundFrequency)
                {
                    PlayTalkingSound(voiceType);
                    timeSinceLastSound = 0f;
                }
                
                yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
                timeSinceLastSound += delay;
            }
        }
        
        // Stop the looping sound when typing is done
        if (useLoopingTalkSound)
        {
            StopLoopingTalkSound();
        }
        
        isTyping = false;
        canAdvance = true;
        
        // Check for immediate transition after typing completes
        if (currentLine != null && currentLine.transitionAfterLine && !string.IsNullOrEmpty(currentLine.transitionToLevel))
        {
            CheckAndTriggerTransition(currentLine);
        }
        // If auto-advance is enabled and we're part of a set, move to next line
        else if (autoAdvance)
        {
            yield return new WaitForSeconds(waitTimeAfterLine);
            
            // If there are more lines in the set, advance to the next one
            if (isPartOfSet && currentSet != null && currentLineIndex < currentSet.narrationLines.Count - 1)
            {
                Debug.Log($"Auto-advancing to next line in set. Current index: {currentLineIndex}, Total lines: {currentSet.narrationLines.Count}");
                AdvanceNarration();
            }
            else if (!isPartOfSet)
            {
                // If not part of a set, just end the narration
                AdvanceNarration();
            }
        }
    }
}