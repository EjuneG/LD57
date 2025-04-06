using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FrameSetDefinition
{
    public string setName;
    public string resourcePath;
}

public class FrameSetManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FOVImageController fovController;
    
    [Header("Frame Sets")]
    [SerializeField] private List<FrameSetDefinition> frameSets = new List<FrameSetDefinition>();
    [SerializeField] private string initialFrameSet;
    
    private string currentFrameSetName;
    
    // Public property to access current frame set name
    public string CurrentFrameSetName => currentFrameSetName;
    
    private void Start()
    {
        // Find the FOV controller if not assigned
        if (fovController == null)
        {
            fovController = FindObjectOfType<FOVImageController>();
            if (fovController == null)
            {
                Debug.LogError("FrameSetManager: No FOVImageController found in scene!");
                return;
            }
        }
        
        // Load initial frame set if specified
        if (!string.IsNullOrEmpty(initialFrameSet))
        {
            SwitchToFrameSet(initialFrameSet, false);
        }
    }
    
    // Switch to a different frame set by name
    public void SwitchToFrameSet(string setName, bool preserveFrameIndex = true)
    {
        if (fovController == null) return;
        
        // Find the frame set with the given name
        FrameSetDefinition frameSet = frameSets.Find(fs => fs.setName == setName);
        
        if (frameSet != null)
        {
            currentFrameSetName = setName;
            LoadFrameSet(frameSet.resourcePath, preserveFrameIndex);
            
            // Trigger event for frame set change
            GameEvents.TriggerOnFrameSetChanged(setName);
        }
        else
        {
            Debug.LogWarning($"FrameSetManager: Frame set '{setName}' not found!");
        }
    }
    
    // Method to load a frame set by path
    private void LoadFrameSet(string resourcePath, bool preserveFrameIndex)
    {
        // Store the current frame index
        int currentIndex = preserveFrameIndex ? fovController.CurrentFrameIndex : 0;
        
        // Load the new frame set
        Sprite[] newFrames = Resources.LoadAll<Sprite>(resourcePath);
        
        // Error checking
        if (newFrames == null || newFrames.Length == 0)
        {
            Debug.LogError($"FrameSetManager: No sprites found at Resources/{resourcePath}");
            return;
        }
        
        // Sort sprites by name
        Array.Sort(newFrames, (a, b) => a.name.CompareTo(b.name));
        
        // Update frame set in the FOV controller
        fovController.UpdateFrameSet(newFrames, currentIndex);
        
        Debug.Log($"FrameSetManager: Loaded {newFrames.Length} frames from {resourcePath}");
    }
}