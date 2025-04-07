using System;
using UnityEngine;

/// <summary>
/// Global event manager that handles events across the game
/// Simplified to remove win condition dependencies
/// </summary>
public static class GameEvents
{
    // Frame and interaction events
    public static event Action<int> OnFrameChanged;
    public static event Action<string> OnObjectInteracted;
    public static event Action<string> OnFrameSetChanged;
    
    // Level transition events
    public static event Action<string> OnLevelTransition;
    public static event Action<string> OnLevelEvent;
    
    // Button events
    public static event Action<string, FlagType> OnButtonPressed;
    
    /// <summary>
    /// Trigger frame changed event
    /// </summary>
    public static void TriggerOnFrameChanged(int frameIndex)
    {
        OnFrameChanged?.Invoke(frameIndex);
    }
    
    /// <summary>
    /// Trigger object interacted event
    /// </summary>
    public static void TriggerOnObjectInteracted(string objectId)
    {
        OnObjectInteracted?.Invoke(objectId);
    }
    
    /// <summary>
    /// Trigger frame set changed event
    /// </summary>
    public static void TriggerOnFrameSetChanged(string frameSetName)
    {
        OnFrameSetChanged?.Invoke(frameSetName);
    }
    
    /// <summary>
    /// Trigger level transition event
    /// </summary>
    public static void TriggerOnLevelTransition(string levelName)
    {
        OnLevelTransition?.Invoke(levelName);
    }
    
    /// <summary>
    /// Trigger a custom level event
    /// </summary>
    public static void TriggerOnLevelEvent(string eventId)
    {
        OnLevelEvent?.Invoke(eventId);
    }
    
    /// <summary>
    /// Trigger button pressed event
    /// </summary>
    public static void TriggerOnButtonPressed(string buttonId, FlagType flagType)
    {
        OnButtonPressed?.Invoke(buttonId, flagType);
    }
}