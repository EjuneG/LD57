using System;

/// <summary>
/// Static class for game-wide events
/// </summary>
public static class GameEvents
{
    // Frame events
    public static event Action<int> OnFrameChanged;
    public static event Action<string> OnFrameSetChanged;
    
    // Object interaction events
    public static event Action<string> OnObjectInteracted;
    
    // Level events
    public static event Action<string> OnLevelEvent;
    public static event Action<string> OnLevelTransition;
    
    // Win condition events
    public static event Action<bool> OnFlagMarked;
    public static event Action<string, FlagType> OnButtonPressed;
    
    // Trigger methods
    public static void TriggerOnFrameChanged(int frameIndex)
    {
        OnFrameChanged?.Invoke(frameIndex);
    }
    
    public static void TriggerOnFrameSetChanged(string frameSetName)
    {
        OnFrameSetChanged?.Invoke(frameSetName);
    }
    
    public static void TriggerOnObjectInteracted(string objectId)
    {
        OnObjectInteracted?.Invoke(objectId);
    }
    
    public static void TriggerOnLevelEvent(string eventId)
    {
        OnLevelEvent?.Invoke(eventId);
    }
    
    public static void TriggerOnLevelTransition(string levelName)
    {
        OnLevelTransition?.Invoke(levelName);
    }
    
    public static void TriggerOnFlagMarked(bool isGreenFlag)
    {
        OnFlagMarked?.Invoke(isGreenFlag);
    }
    
    public static void TriggerOnButtonPressed(string buttonId, FlagType flagType)
    {
        OnButtonPressed?.Invoke(buttonId, flagType);
        
        // Also trigger the flag marked event for convenience
        if (flagType != FlagType.None)
        {
            TriggerOnFlagMarked(flagType == FlagType.GreenFlag);
        }
    }
}