using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StorySlide", menuName = "Scriptable Objects/StorySlide")]
public class StorySlide : ScriptableObject
{
    [Header("Visual")]
    public Sprite image;
    
    [Header("Text")]
    [TextArea(3, 10)]
    public string text;
    public Color textColor = Color.white;
    
    [Header("Timing")]
    public float displayDuration = 3f; // How long to wait after text is displayed before auto-advancing
}