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
    private bool isLoadingFrameSet = false;
    
    // Public property to access current frame set name
    public string CurrentFrameSetName => currentFrameSetName;
    
    // Public property to check if we're currently loading a frame set
    // This allows other components to check this status
    public bool IsLoadingFrameSet => isLoadingFrameSet;
    
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
        // Prevent recursive calls - this is critical to avoid stack overflow
        if (isLoadingFrameSet)
        {
            Debug.LogWarning("FrameSetManager: Recursive call to LoadFrameSet detected and prevented!");
            return;
        }
        
        isLoadingFrameSet = true;
        
        try
        {
            // Store the current frame index if needed
            int currentIndex = preserveFrameIndex ? fovController.CurrentFrameIndex : 0;
            
            // Load the new frame set
            Sprite[] newFrames = Resources.LoadAll<Sprite>(resourcePath);
            
            // Error checking
            if (newFrames == null || newFrames.Length == 0)
            {
                Debug.LogError($"FrameSetManager: No sprites found at Resources/{resourcePath}");
                isLoadingFrameSet = false;
                return;
            }
            
            // Sort sprites by name - Using a simple index-based approach instead of name comparison
            // This avoids potential issues with sprite name properties
            try 
            {
                // Sort using a simple numeric extraction from the sprite name if possible
                // This is often more reliable than string comparison with Unity objects
                Array.Sort(newFrames, (a, b) => {
                    if (a == null && b == null) return 0;
                    if (a == null) return -1;
                    if (b == null) return 1;
                    
                    // Try to extract numbers from the end of the sprite names (e.g., "sprite_001")
                    string nameA = a.name;
                    string nameB = b.name;
                    
                    return string.Compare(nameA, nameB, StringComparison.Ordinal);
                });
            }
            catch (Exception ex)
            {
                // If sorting fails, log a warning but continue with unsorted frames
                Debug.LogWarning($"FrameSetManager: Failed to sort sprites: {ex.Message}. Using unsorted order.");
            }
            
            // Update frame set in the FOV controller
            fovController.UpdateFrameSet(newFrames, currentIndex);
            
            Debug.Log($"FrameSetManager: Loaded {newFrames.Length} frames from {resourcePath}, starting at frame {currentIndex}");
        }
        finally
        {
            // Always reset the loading flag, even if an exception occurs
            isLoadingFrameSet = false;
        }
    }
}