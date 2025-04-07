using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NarrationLine", menuName = "Scriptable Objects/NarrationLine")]

[Serializable]
public class VoiceOver
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float pitch = 1f;
}

public class NarrationLine : ScriptableObject
{
    [TextArea(3, 10)]
    public string text;
    public Color textcolor = Color.white;
    
    [Header("Audio")]
    public VoiceOver voiceOver;
    
    [Header("Talking Sound")]
    [Tooltip("Type of character voice for talking sound effects")]
    public NarrationVoiceType voiceType = NarrationVoiceType.Normal;
    
    [Header("Level Transition")]
    [Tooltip("If set, the game will transition to this level after this line")]
    public string transitionToLevel;
    public bool transitionAfterLine = false;
}

// Enum to define the type of character voice
public enum NarrationVoiceType
{
    Normal,
    Evil
}