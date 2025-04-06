using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NarrationSet", menuName = "Scriptable Objects/NarrationSet")]
public class NarrationSet : ScriptableObject
{
    public List<NarrationLine> narrationLines;
    public bool autoPlayNext = true;

    [Header("Level Transition")]
    [Tooltip("If set, transitions to this level after the last line")]
    public string transitionToLevel;
    public bool transitionAfterSet = false;

    [Header("Metadata")]
    public string setId;
    public string displayName;
}