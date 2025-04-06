using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages level data and connects it to scene buttons using an ID-based system
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData levelData;

    [Header("References")]
    [SerializeField] private FOVImageController fovController;
    [SerializeField] private NarrationManager narrationManager;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Runtime data
    private Dictionary<string, FrameSensitiveButton> activeButtons = new Dictionary<string, FrameSensitiveButton>();
    private string currentFrameSet = "";
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    private void OnEnable()
    {
        // Subscribe to events
        GameEvents.OnFrameChanged += OnFrameChanged;
        GameEvents.OnObjectInteracted += OnObjectInteracted;
        GameEvents.OnFrameSetChanged += OnFrameSetChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnFrameChanged -= OnFrameChanged;
        GameEvents.OnObjectInteracted -= OnObjectInteracted;
        GameEvents.OnFrameSetChanged -= OnFrameSetChanged;
    }

    private void Start()
    {
        // Find components if not assigned
        if (fovController == null)
            fovController = FindObjectOfType<FOVImageController>();

        if (narrationManager == null)
            narrationManager = FindObjectOfType<NarrationManager>();

        // Load level data
        if (levelData != null)
        {
            LoadLevelData();
        }
        else
        {
            Debug.LogError("LevelManager: No LevelData assigned!");
        }
    }

    /// <summary>
    /// Load the level data and set up the level
    /// </summary>
    public void LoadLevelData()
    {
        // First, deactivate any active buttons
        DeactivateAllButtons();

        // Initialize frame sets
        if (levelData.frameSets.Count > 0)
        {
            // Create frame set manager if needed
            FrameSetManager frameSetManager = GetComponent<FrameSetManager>();
            if (frameSetManager == null)
            {
                frameSetManager = gameObject.AddComponent<FrameSetManager>();
            }

            // Set up frame sets
            SetupFrameSetManager(frameSetManager);

            // Record initial frame set
            currentFrameSet = levelData.initialFrameSet;
        }

        // Find and set up all buttons from the level data
        SetupButtons();

        // Reset all frame event triggers
        foreach (var frameEvent in levelData.frameEvents)
        {
            frameEvent.hasTriggered = false;
        }
    }

    /// <summary>
    /// Set up the frame set manager with the level data
    /// </summary>
    private void SetupFrameSetManager(FrameSetManager frameSetManager)
    {
        // Use reflection since we only need to do this once at startup
        // Set initial frame set
        var initialFrameSetField = frameSetManager.GetType().GetField("initialFrameSet");
        if (initialFrameSetField != null)
        {
            initialFrameSetField.SetValue(frameSetManager, levelData.initialFrameSet);
        }

        // Set frame sets
        var frameSetsField = frameSetManager.GetType().GetField("frameSets");
        if (frameSetsField != null)
        {
            // Create a new list that matches the type expected by FrameSetManager
            var frameSetsList = System.Activator.CreateInstance(frameSetsField.FieldType);

            // Get the Add method
            var addMethod = frameSetsList.GetType().GetMethod("Add");

            // For each frame set in level data, create a new one for the manager
            foreach (var frameSet in levelData.frameSets)
            {
                var newFrameSet = System.Activator.CreateInstance(typeof(FrameSetDefinition));

                // Set properties using reflection
                var setNameField = newFrameSet.GetType().GetField("setName");
                var resourcePathField = newFrameSet.GetType().GetField("resourcePath");

                if (setNameField != null && resourcePathField != null)
                {
                    setNameField.SetValue(newFrameSet, frameSet.setName);
                    resourcePathField.SetValue(newFrameSet, frameSet.resourcePath);

                    // Add to the list
                    addMethod.Invoke(frameSetsList, new object[] { newFrameSet });
                }
            }

            // Set the frameSets field
            frameSetsField.SetValue(frameSetManager, frameSetsList);
        }
    }

    /// <summary>
    /// Find and set up all buttons based on their IDs
    /// </summary>
    private void SetupButtons()
    {
        // Find all FrameSensitiveButtons in the scene (including inactive)
        FrameSensitiveButton[] sceneButtons = FindObjectsOfType<FrameSensitiveButton>(true);

        if (debugMode)
        {
            Debug.Log($"Found {sceneButtons.Length} FrameSensitiveButtons in scene");
        }

        // Create a dictionary for quick lookup by ID
        Dictionary<string, FrameSensitiveButton> buttonLookup = new Dictionary<string, FrameSensitiveButton>();
        foreach (var button in sceneButtons)
        {
            string id = button.ObjectId;
            if (!string.IsNullOrEmpty(id))
            {
                buttonLookup[id] = button;

                if (debugMode)
                {
                    Debug.Log($"  Button: {button.name}, ID: {id}, Active: {button.gameObject.activeSelf}");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning($"  Button {button.name} has no ObjectId set!");
                }
            }
        }

        // Process button configs from level data
        foreach (var config in levelData.buttonConfigs)
        {
            string buttonId = config.buttonId;

            // Skip if ID is empty
            if (string.IsNullOrEmpty(buttonId))
            {
                Debug.LogWarning("LevelManager: Skipping button config with empty ID");
                continue;
            }

            // Try to find the button in the scene
            if (buttonLookup.TryGetValue(buttonId, out FrameSensitiveButton button))
            {
                // Set up the button
                SetupButton(button, config);

                if (debugMode)
                {
                    Debug.Log($"Set up button {button.name} with ID {buttonId}");
                }
            }
            else
            {
                Debug.LogWarning($"LevelManager: Button with ID '{buttonId}' not found in scene");
            }
        }
    }

    /// <summary>
    /// Set up a single button with its configuration
    /// </summary>
    private void SetupButton(FrameSensitiveButton button, ButtonConfig config)
    {
        // Clear any existing listeners
        button.ClearListeners();

        // Add a new listener for this level
        button.AddListener(() => HandleButtonInteraction(config));

        // Activate the button
        button.gameObject.SetActive(true);

        // Store in active buttons dictionary
        activeButtons[config.buttonId] = button;
    }

    /// <summary>
    /// Deactivate all previously activated buttons
    /// </summary>
    private void DeactivateAllButtons()
    {
        foreach (var button in activeButtons.Values)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        activeButtons.Clear();
    }

    /// <summary>
    /// Handle frame change events
    /// </summary>
    private void OnFrameChanged(int frameIndex)
    {
        // Check for frame event triggers
        foreach (var frameEvent in levelData.frameEvents)
        {
            if (frameEvent.frameIndex == frameIndex &&
                (string.IsNullOrEmpty(frameEvent.frameSetName) || frameEvent.frameSetName == currentFrameSet))
            {
                // Skip if this should only trigger once and already has
                if (frameEvent.triggerOnce && frameEvent.hasTriggered)
                    continue;

                // Mark as triggered
                frameEvent.hasTriggered = true;

                // Handle the event based on type
                HandleFrameEvent(frameEvent);
            }
        }
    }

    /// <summary>
    /// Handle a frame event trigger
    /// </summary>
    private void HandleFrameEvent(FrameEventTrigger frameEvent)
    {
        switch (frameEvent.eventType)
        {
            case FrameEventType.PlayNarration:
                PlayNarration(frameEvent.narrationLine);
                break;

            case FrameEventType.PlayNarrationSet:
                PlayNarrationSet(frameEvent.narrationSet);
                break;

            case FrameEventType.SwitchFrameSet:
                // Use the targetFrameSet field
                SwitchFrameSet(frameEvent.targetFrameSet);
                break;

            case FrameEventType.PlaySound:
                PlaySound(frameEvent.customEventId);
                break;

            case FrameEventType.StartAnimation:
                TriggerAnimation(frameEvent.customEventId);
                break;

            case FrameEventType.TransitionToLevel:
                TransitionToLevel(frameEvent.targetLevel);
                break;

            case FrameEventType.Custom:
                GameEvents.TriggerOnLevelEvent(frameEvent.customEventId);
                break;
        }
    }

    /// <summary>
    /// Handle object interaction events
    /// </summary>
    private void OnObjectInteracted(string objectId)
    {
        // This is already handled by the button's own event handler
        // But you could add additional global handling here if needed
        if (debugMode)
        {
            Debug.Log($"Object interaction event: {objectId}");
        }
    }

    /// <summary>
    /// Handle frame set change events
    /// </summary>
    private void OnFrameSetChanged(string frameSetName)
    {
        currentFrameSet = frameSetName;
        if (debugMode)
        {
            Debug.Log($"Frame set changed to: {frameSetName}");
        }
    }

    /// <summary>
    /// Handle button interactions
    /// </summary>
    private void HandleButtonInteraction(ButtonConfig config)
    {
        // Skip if this button shouldn't be active in the current frame set
        if (!string.IsNullOrEmpty(config.frameSetName) &&
            config.frameSetName != currentFrameSet)
            return;

        switch (config.actionType)
        {
            case InteractionActionType.PlayNarration:
                PlayNarration(config.narrationLine);
                break;

            case InteractionActionType.PlayNarrationSet:
                PlayNarrationSet(config.narrationSet);
                break;

            case InteractionActionType.SwitchFrameSet:
                SwitchFrameSet(config.targetFrameSet);
                break;

            case InteractionActionType.PlaySound:
                PlaySound(config.customEventId);
                break;

            case InteractionActionType.TriggerAnimation:
                TriggerAnimation(config.customEventId);
                break;

            case InteractionActionType.Custom:
                GameEvents.TriggerOnLevelEvent(config.customEventId);
                break;
        }
    }

    /// <summary>
    /// Play a narration line
    /// </summary>
    private void PlayNarration(NarrationLine narrationLine)
    {
        if (narrationManager == null || narrationLine == null) return;

        narrationManager.PlayLine(narrationLine);
    }

    /// <summary>
    /// Play a narration set
    /// </summary>
    private void PlayNarrationSet(NarrationSet narrationSet)
    {
        if (narrationManager == null || narrationSet == null) return;

        narrationManager.PlayNarrationSet(narrationSet);
    }

    /// <summary>
    /// Switch to a different frame set
    /// </summary>
    private void SwitchFrameSet(string frameSetName)
    {
        if (string.IsNullOrEmpty(frameSetName))
        {
            Debug.LogWarning("LevelManager: Cannot switch to empty frame set name!");
            return;
        }

        FrameSetManager frameSetManager = GetComponent<FrameSetManager>();
        if (frameSetManager != null)
        {
            frameSetManager.SwitchToFrameSet(frameSetName);
        }
        else
        {
            Debug.LogWarning("LevelManager: Cannot switch frame set - FrameSetManager not found!");
        }
    }

    /// <summary>
    /// Play a sound by ID
    /// </summary>
    private void PlaySound(string soundId)
    {
        // Get or create audio source
        if (!audioSources.TryGetValue(soundId, out AudioSource source))
        {
            // Try to find audio clip
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{soundId}");
            if (clip != null)
            {
                GameObject audioObj = new GameObject($"Sound_{soundId}");
                audioObj.transform.SetParent(transform);
                source = audioObj.AddComponent<AudioSource>();
                source.clip = clip;
                audioSources[soundId] = source;
            }
            else
            {
                Debug.LogWarning($"LevelManager: Cannot find audio clip: {soundId}");
                return;
            }
        }

        // Play the sound
        if (source != null && source.clip != null)
        {
            source.Play();
        }
    }

    /// <summary>
    /// Trigger an animation by ID
    /// </summary>
    private void TriggerAnimation(string animationId)
    {
        // Find animator with this ID
        Animator animator = GameObject.Find(animationId)?.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Play");
        }
        else
        {
            Debug.LogWarning($"LevelManager: Cannot find animator: {animationId}");
        }
    }

    /// <summary>
    /// Transition to another level
    /// </summary>
    private void TransitionToLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogWarning("LevelManager: Cannot transition to empty level name!");
            return;
        }

        // Trigger the level transition event
        GameEvents.TriggerOnLevelTransition(levelName);
    }
}