using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Information")]
    public string levelName;
    public string levelDescription;
    
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
    
    [Tooltip("The frame set this button applies to (leave empty for any frame set)")]
    public string frameSetName;
    
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
    Custom
}