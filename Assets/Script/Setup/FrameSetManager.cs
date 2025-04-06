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
            SwitchToFrameSet(initialFrameSet, false); // Start at frame 0 for initial load
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
            
            // When switching frame sets (not changing levels), we preserve the frame index by default
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
        // Store the current frame index if needed
        int currentIndex = preserveFrameIndex ? fovController.CurrentFrameIndex : 0;
        
        // Load the new frame set
        Sprite[] newFrames = Resources.LoadAll<Sprite>(resourcePath);
        
        // Error checking
        if (newFrames == null || newFrames.Length == 0)
        {
            Debug.LogError($"FrameSetManager: No sprites found at Resources/{resourcePath}");
            return;
        }
        
        // Sort sprites by name - FIXED: Using a safer string comparison to avoid stack overflow
        try {
            Array.Sort(newFrames, (a, b) => {
                // Null checks to be extra safe
                if (a == null && b == null) return 0;
                if (a == null) return -1;
                if (b == null) return 1;
                
                // Use string.Compare instead of CompareTo for safety
                return string.Compare(a.name, b.name, StringComparison.Ordinal);
            });
        }
        catch (Exception ex) {
            // If sorting fails, log a warning but continue with unsorted frames
            Debug.LogWarning($"FrameSetManager: Failed to sort sprites: {ex.Message}. Using unsorted order.");
        }
        
        // Update frame set in the FOV controller
        fovController.UpdateFrameSet(newFrames, currentIndex);
        
        Debug.Log($"FrameSetManager: Loaded {newFrames.Length} frames from {resourcePath}, starting at frame {currentIndex}");
    }
}