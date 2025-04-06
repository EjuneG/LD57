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
    
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        // Stop any existing narration
        StopAllCoroutines();
        isTyping = false;
        
        // Track the current line being played
        // We need to explicitly set this for single lines outside of sets
        currentLineIndex = 0;
        currentSet = ScriptableObject.CreateInstance<NarrationSet>();
        currentSet.narrationLines = new List<NarrationLine> { line };
        
        // Start typing
        typingCoroutine = StartCoroutine(TypeText(line.text));
        
        // Play voice over if available
        if (line.voiceOver != null && audioSource != null)
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
        
        currentSet = set;
        currentLineIndex = 0;
        
        // Play the first line
        PlayLine(set.narrationLines[currentLineIndex]);
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
            if (textDisplay != null && currentSet != null && currentLineIndex < currentSet.narrationLines.Count)
            {
                textDisplay.text = currentSet.narrationLines[currentLineIndex].text;
            }
            isTyping = false;
            canAdvance = true;
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
        PlayLine(currentSet.narrationLines[currentLineIndex]);
    }
    
    /// <summary>
    /// End the narration and hide the text box
    /// </summary>
    public void EndNarration()
    {
        StopAllCoroutines();
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
        var lastLine = currentSet != null && currentLineIndex < currentSet.narrationLines.Count 
            ? currentSet.narrationLines[currentLineIndex] 
            : null;
            
        currentSet = null;
        isTyping = false;
        canAdvance = false;
        
        // Trigger the line completion event
        if (lastLine != null)
        {
            OnNarrationLineCompleted?.Invoke(lastLine);
        }
    }
    
    /// <summary>
    /// Typewriter effect coroutine with variable typing speed
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        canAdvance = false;
        
        if (textDisplay != null)
        {
            textDisplay.text = "";
            
            for (int i = 0; i < text.Length; i++)
            {
                // Add one character at a time
                textDisplay.text += text[i];
                
                // Creepy variable timing for typewriter effect
                float delay = baseTypeSpeed;
                
                // Add variation to make it feel more organic/creepy
                delay += UnityEngine.Random.Range(-speedVariation, speedVariation);
                
                // Longer pauses on punctuation
                if (text[i] == '.' || text[i] == ',' || text[i] == '?' || text[i] == '!')
                {
                    delay += pauseOnPunctuation;
                }
                
                yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
            }
        }
        
        isTyping = false;
        canAdvance = true;
        
        // Auto-advance after a delay if enabled
        if (autoAdvance)
        {
            yield return new WaitForSeconds(waitTimeAfterLine);
            AdvanceNarration();
        }
    }
}