using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages level data and connects it to scene buttons using an ID-based system
/// Simplified to remove WinConditionManager dependency
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData levelData;

    [Header("References")]
    [SerializeField] private FOVImageController fovController;
    [SerializeField] private NarrationManager narrationManager;
    [SerializeField] private Transform buttonTriggersParent; // Reference to the parent containing all ButtonTriggers
    [SerializeField] private AudioManager audioManager; // Reference to the AudioManager

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Runtime data
    private Dictionary<string, FrameSensitiveButton> activeButtons = new Dictionary<string, FrameSensitiveButton>();
    private string currentFrameSet = "";
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
    private GameObject currentActiveButtonParent; // Track current active button parent

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

        if (audioManager == null)
            audioManager = AudioManager.Instance;

        // Find ButtonTriggers parent if not assigned
        if (buttonTriggersParent == null)
            buttonTriggersParent = GameObject.Find("ButtonTriggers")?.transform;

        if (buttonTriggersParent == null)
            Debug.LogWarning("LevelManager: ButtonTriggers parent not found!");

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

        // Apply level-specific settings to FOV controller
        if (fovController != null)
        {
            fovController.SetDragDirection(levelData.dragDirection);
        }

        // Play background music if specified
        PlayLevelBackgroundMusic();

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

            // IMPORTANT: For new level loads, force start at frame 0
            frameSetManager.SwitchToFrameSet(levelData.initialFrameSet, false);
        }

        // Find and set up all buttons from the level data
        SetupButtonsForLevel();

        // Reset all frame event triggers
        foreach (var frameEvent in levelData.frameEvents)
        {
            frameEvent.hasTriggered = false;
        }
    }

    /// <summary>
    /// Play the background music specified in the level data
    /// </summary>
    private void PlayLevelBackgroundMusic()
    {
        // Only proceed if we have an audio manager and level data
        if (audioManager == null || levelData == null)
            return;

        // If a background music is specified, play it
        if (!string.IsNullOrEmpty(levelData.backgroundMusic))
        {
            audioManager.PlayBGM(levelData.backgroundMusic);
            
            if (debugMode)
            {
                Debug.Log($"LevelManager: Playing background music: {levelData.backgroundMusic}");
            }
        }
        else if (debugMode)
        {
            Debug.Log("LevelManager: No background music specified, keeping current track");
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
    /// Find and set up buttons for the current level
    /// </summary>
    private void SetupButtonsForLevel()
    {
        if (buttonTriggersParent == null)
        {
            Debug.LogError("LevelManager: ButtonTriggers parent not found!");
            return;
        }

        // Deactivate all ButtonTriggers initially
        foreach (Transform child in buttonTriggersParent)
        {
            child.gameObject.SetActive(false);
        }

        // Find the ButtonTrigger for the current level
        string targetLevelName = levelData.levelName;
        Transform levelParent = null;

        foreach (Transform child in buttonTriggersParent)
        {
            if (child.name.Equals(targetLevelName, System.StringComparison.OrdinalIgnoreCase))
            {
                levelParent = child;
                break;
            }
        }

        if (levelParent == null)
        {
            Debug.LogWarning($"LevelManager: ButtonTrigger parent for level '{targetLevelName}' not found!");
            return;
        }

        // Activate the parent for this level
        levelParent.gameObject.SetActive(true);
        currentActiveButtonParent = levelParent.gameObject;

        if (debugMode)
        {
            Debug.Log($"Activated ButtonTrigger parent for level: {targetLevelName}");
        }

        // Find all FrameSensitiveButtons under this parent
        FrameSensitiveButton[] buttonsInLevel = levelParent.GetComponentsInChildren<FrameSensitiveButton>(true);

        if (debugMode)
        {
            Debug.Log($"Found {buttonsInLevel.Length} FrameSensitiveButtons in level {targetLevelName}");
        }

        // Create a dictionary for quick lookup by ID
        Dictionary<string, FrameSensitiveButton> buttonLookup = new Dictionary<string, FrameSensitiveButton>();
        foreach (var button in buttonsInLevel)
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

            // Try to find the button in the current level
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
                Debug.LogWarning($"LevelManager: Button with ID '{buttonId}' not found in current level");
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

        // Set button active state based on config
        button.gameObject.SetActive(config.activeAtStart);

        // Store in active buttons dictionary (even if inactive)
        activeButtons[config.buttonId] = button;
    }

    /// <summary>
    /// Deactivate all previously activated buttons
    /// </summary>
    private void DeactivateAllButtons()
    {
        // Deactivate any previous active parent
        if (currentActiveButtonParent != null)
        {
            currentActiveButtonParent.SetActive(false);
            currentActiveButtonParent = null;
        }

        // Also clean up the dictionary
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
    /// Handle button interactions with proper branching
    /// </summary>
    private void HandleButtonInteraction(ButtonConfig config)
    {
        // Skip if this button shouldn't be active in the current frame set
        if (!string.IsNullOrEmpty(config.frameSetName) &&
            config.frameSetName != currentFrameSet)
            return;

        // Simplified flag handling - directly use the customNextLevel if specified
        string targetLevel = null;
        
        if (config.flagType != FlagType.None)
        {
            // Use customNextLevel if specified, otherwise use the regular targetLevel
            if (!string.IsNullOrEmpty(config.customNextLevel))
            {
                targetLevel = config.customNextLevel;
            }
        }

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

            case InteractionActionType.SetButtonActive:
                SetButtonActive(config.customEventId, true); // Use customEventId as buttonId to set active
                break;

            case InteractionActionType.TransitionToLevel:
                // First check if we have a flag-based target level
                if (targetLevel != null)
                {
                    GameEvents.TriggerOnLevelTransition(targetLevel);
                }
                // Otherwise, use the standard target level
                else if (!string.IsNullOrEmpty(config.targetLevel))
                {
                    GameEvents.TriggerOnLevelTransition(config.targetLevel);
                }
                else
                {
                    Debug.LogWarning("Button tried to transition level but no target level specified!");
                }
                break;

            case InteractionActionType.Custom:
                GameEvents.TriggerOnLevelEvent(config.customEventId);
                break;
        }
        
        // If the button has a flag type and isn't transitioning directly, trigger the level transition
        if (config.flagType != FlagType.None && config.actionType != InteractionActionType.TransitionToLevel)
        {
            if (!string.IsNullOrEmpty(config.customNextLevel))
            {
                GameEvents.TriggerOnLevelTransition(config.customNextLevel);
            }
        }
    }

    /// <summary>
    /// Handle a frame event trigger
    /// </summary>
    private void HandleFrameEvent(FrameEventTrigger frameEvent)
    {
        // Simplified flag handling - directly transition based on flag type
        string targetLevel = null;
        
        if (frameEvent.flagType != FlagType.None)
        {
            // Use customNextLevel if specified, otherwise use default level flow
            if (!string.IsNullOrEmpty(frameEvent.customNextLevel))
            {
                targetLevel = frameEvent.customNextLevel;
            }
        }

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

            case FrameEventType.SetButtonActive:
                SetButtonActive(frameEvent.targetButtonId, frameEvent.setButtonActive);
                break;

            case FrameEventType.TransitionToLevel:
                // First check if we have a flag-based target level
                if (targetLevel != null)
                {
                    GameEvents.TriggerOnLevelTransition(targetLevel);
                }
                // Otherwise, use the standard target level
                else if (!string.IsNullOrEmpty(frameEvent.targetLevel))
                {
                    GameEvents.TriggerOnLevelTransition(frameEvent.targetLevel);
                }
                else
                {
                    Debug.LogWarning("Frame event tried to transition level but no target level specified!");
                }
                break;

            case FrameEventType.Custom:
                GameEvents.TriggerOnLevelEvent(frameEvent.customEventId);
                break;
        }
        
        // If the frame event has a flag type and isn't transitioning directly, trigger the level transition
        if (frameEvent.flagType != FlagType.None && frameEvent.eventType != FrameEventType.TransitionToLevel)
        {
            if (!string.IsNullOrEmpty(frameEvent.customNextLevel))
            {
                GameEvents.TriggerOnLevelTransition(frameEvent.customNextLevel);
            }
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
    /// Set a button active or inactive by ID
    /// </summary>
    private void SetButtonActive(string buttonId, bool active)
    {
        if (string.IsNullOrEmpty(buttonId))
        {
            Debug.LogWarning("LevelManager: Cannot set button active - empty button ID!");
            return;
        }

        // First try to find the button in our dictionary of active buttons
        if (activeButtons.TryGetValue(buttonId, out FrameSensitiveButton button))
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
                if (debugMode)
                {
                    Debug.Log($"LevelManager: Button '{buttonId}' set {(active ? "active" : "inactive")}");
                }
                return;
            }
        }

        // If not found in dictionary, search for it in the current level's button triggers
        if (currentActiveButtonParent != null)
        {
            FrameSensitiveButton[] buttons = currentActiveButtonParent.GetComponentsInChildren<FrameSensitiveButton>(true);
            foreach (var fsButton in buttons)
            {
                if (fsButton.ObjectId == buttonId)
                {
                    fsButton.gameObject.SetActive(active);

                    // Add to dictionary for future reference
                    activeButtons[buttonId] = fsButton;

                    if (debugMode)
                    {
                        Debug.Log($"LevelManager: Button '{buttonId}' set {(active ? "active" : "inactive")} (found by search)");
                    }
                    return;
                }
            }
        }

        Debug.LogWarning($"LevelManager: Button with ID '{buttonId}' not found in level!");
    }
}