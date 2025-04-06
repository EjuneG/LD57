using System;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    // Frame change event
    public static event Action<int> OnFrameChanged;
    
    // Frame set change event
    public static event Action<string> OnFrameSetChanged;
    
    // Object interaction events
    public static event Action<string> OnObjectInteracted;
    
    // Level events
    public static event Action<string> OnLevelEvent;
    
    // Level transition event
    public static event Action<string> OnLevelTransition;
    
    // Trigger methods
    public static void TriggerOnFrameChanged(int frame) => OnFrameChanged?.Invoke(frame);
    public static void TriggerOnFrameSetChanged(string frameSetName) => OnFrameSetChanged?.Invoke(frameSetName);
    public static void TriggerOnObjectInteracted(string objectId) => OnObjectInteracted?.Invoke(objectId);
    public static void TriggerOnLevelEvent(string eventName) => OnLevelEvent?.Invoke(eventName);
    public static void TriggerOnLevelTransition(string levelName) => OnLevelTransition?.Invoke(levelName);
}