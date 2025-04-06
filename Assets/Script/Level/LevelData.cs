using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Information")]
    public string levelName;
    public string levelDescription;

    [Header("FOV Control Settings")]
    [Tooltip("Whether to invert the drag direction for this level (true = drag down to move forward)")]
    public bool invertDrag = false;

    [Header("Frame Sets")]
    public string initialFrameSet;
    public List<FrameSetDefinition> frameSets = new List<FrameSetDefinition>();

    [Header("Frame Events")]
    public List<FrameEventTrigger> frameEvents = new List<FrameEventTrigger>();

    [Header("Interactive Buttons")]
    [Tooltip("Configuration for buttons in the scene")]
    public List<ButtonConfig> buttonConfigs = new List<ButtonConfig>();
}

[Serializable]
public class ButtonConfig
{
    [Tooltip("Button ID - must match the ObjectId of a FrameSensitiveButton in the scene")]
    public string buttonId;

    [Tooltip("Display name (for editor organization)")]
    public string displayName;

    [Tooltip("Whether this button should be active when the level starts")]
    public bool activeAtStart = true;

    [Tooltip("The frame set this button applies to (leave empty for any frame set)")]
    public string frameSetName;

    // Rest of the class remains unchanged
    [Tooltip("Action to take when this button is clicked")]
    public InteractionActionType actionType;

    [Tooltip("Custom event ID (use with Custom action type)")]
    public string customEventId;

    [Tooltip("Frame set to switch to (when using SwitchFrameSet action)")]
    public string targetFrameSet;

    [Tooltip("Narration line to play (use with PlayNarration action type)")]
    public NarrationLine narrationLine;

    [Tooltip("Narration set to play (use with PlayNarrationSet action type)")]
    public NarrationSet narrationSet;

    [Tooltip("Target level to transition to (use with TransitionToLevel action type)")]
    public string targetLevel;

    [Header("Win Condition")]
    [Tooltip("Flag type for this button (Green = correct, Red = wrong, None = no impact)")]
    public FlagType flagType = FlagType.None;

    [Tooltip("Custom next level to transition to (overrides default level progression)")]
    public string customNextLevel;
}

[Serializable]
public class FrameEventTrigger
{
    [Tooltip("The frame set this event applies to (leave empty for any frame set)")]
    public string frameSetName;

    [Tooltip("The frame index that triggers this event")]
    public int frameIndex;

    [Tooltip("Event type to trigger")]
    public FrameEventType eventType;

    [Tooltip("Custom event ID (use with Custom event type)")]
    public string customEventId;

    [Tooltip("Target frame set to switch to (use with SwitchFrameSet event type)")]
    public string targetFrameSet;

    [Tooltip("Narration line to play (use with PlayNarration event type)")]
    public NarrationLine narrationLine;

    [Tooltip("Narration set to play (use with PlayNarrationSet event type)")]
    public NarrationSet narrationSet;

    [Tooltip("Target level to transition to (use with TransitionToLevel event type)")]
    public string targetLevel;

    [Tooltip("Should this event trigger only once?")]
    public bool triggerOnce = true;

    [Header("Win Condition")]
    [Tooltip("Flag type for this event (Green = correct, Red = wrong, None = no impact)")]
    public FlagType flagType = FlagType.None;

    [Tooltip("Custom next level to transition to (overrides default level progression)")]
    public string customNextLevel;
    [Tooltip("Button ID to set active/inactive (use with SetButtonActive event type)")]
    public string targetButtonId;

    [Tooltip("Whether to activate (true) or deactivate (false) the button")]
    public bool setButtonActive = true;

    [Tooltip("Has this event been triggered? (Runtime state)")]
    [NonSerialized] public bool hasTriggered = false;
}

[Serializable]
public enum FrameEventType
{
    PlayNarration,
    PlayNarrationSet,
    SwitchFrameSet,
    PlaySound,
    StartAnimation,
    TransitionToLevel,
    SetButtonActive,
    Custom
}

[Serializable]
public enum InteractionActionType
{
    PlayNarration,
    PlayNarrationSet,
    SwitchFrameSet,
    PlaySound,
    TriggerAnimation,
    TransitionToLevel,
    SetButtonActive,
    Custom
}

[Serializable]
public enum FlagType
{
    None,       // No flag impact
    GreenFlag,  // Correct choice
    RedFlag     // Wrong choice
}