using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks player choices across levels to determine win condition
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelBranch
    {
        public string levelName;
        public string nextLevelIfGreenFlag;
        public string nextLevelIfRedFlag;
    }
    
    [Header("Level Branches")]
    [SerializeField] private List<LevelBranch> levelBranches = new List<LevelBranch>();
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Dictionary to store flag state for each level (green = true, red = false)
    private Dictionary<string, bool> levelFlags = new Dictionary<string, bool>();
    
    // Current level name
    private string _currentLevel;
    
    // Public property to access current level name
    public string currentLevel => _currentLevel;
    
    private void Awake()
    {
        // Make sure this persists across level transitions
        DontDestroyOnLoad(this.gameObject);
    }
    
    private void OnEnable()
    {
        // Subscribe to events
        GameEvents.OnLevelTransition += HandleLevelTransition;
        GameEvents.OnFlagMarked += MarkFlag;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnLevelTransition -= HandleLevelTransition;
        GameEvents.OnFlagMarked -= MarkFlag;
    }
    
    /// <summary>
    /// Handle level transition events
    /// </summary>
    private void HandleLevelTransition(string levelName)
    {
        // Only update the current level name
        // Do NOT trigger any additional transitions from here!
        if (debugMode)
        {
            Debug.Log($"WinConditionManager: Current level changed to {levelName}");
        }
        
        _currentLevel = levelName;
    }
    
    /// <summary>
    /// Mark a flag for the current level
    /// </summary>
    public void MarkFlag(bool isGreenFlag)
    {
        if (string.IsNullOrEmpty(_currentLevel))
            return;
            
        levelFlags[_currentLevel] = isGreenFlag;
        
        if (debugMode)
        {
            Debug.Log($"Marked level {_currentLevel} with {(isGreenFlag ? "green" : "red")} flag");
        }
    }
    
    /// <summary>
    /// Mark a flag for a specific level
    /// </summary>
    public void MarkFlagForLevel(string levelName, bool isGreenFlag)
    {
        if (string.IsNullOrEmpty(levelName))
            return;
            
        levelFlags[levelName] = isGreenFlag;
        
        if (debugMode)
        {
            Debug.Log($"Explicitly marked level {levelName} with {(isGreenFlag ? "green" : "red")} flag");
        }
    }
    
    /// <summary>
    /// Get the appropriate next level based on the current level's flag
    /// </summary>
    public string GetNextLevel(string currentLevelName)
    {
        // Find the level branch configuration
        LevelBranch branch = levelBranches.Find(b => b.levelName == currentLevelName);
        
        if (branch == null)
        {
            Debug.LogWarning($"No branch configuration found for level: {currentLevelName}");
            return null;
        }
        
        // Check if we have a flag for this level
        if (levelFlags.TryGetValue(currentLevelName, out bool isGreenFlag))
        {
            // Return the appropriate next level
            string nextLevel = isGreenFlag ? branch.nextLevelIfGreenFlag : branch.nextLevelIfRedFlag;
            
            if (debugMode)
            {
                Debug.Log($"GetNextLevel: From {currentLevelName} with {(isGreenFlag ? "green" : "red")} flag -> {nextLevel}");
            }
            
            return nextLevel;
        }
        
        // Default to green path if no flag is set
        if (debugMode)
        {
            Debug.LogWarning($"No flag set for level: {currentLevelName}, defaulting to green path");
        }
        
        return branch.nextLevelIfGreenFlag;
    }
    
    /// <summary>
    /// Check if the player has achieved the win condition (all levels with green flags)
    /// </summary>
    public bool CheckWinCondition()
    {
        foreach (var levelBranch in levelBranches)
        {
            // If any required level has a red flag, the win condition is not met
            if (levelFlags.TryGetValue(levelBranch.levelName, out bool isGreenFlag) && !isGreenFlag)
            {
                if (debugMode)
                {
                    Debug.Log($"Win condition not met: {levelBranch.levelName} has red flag");
                }
                return false;
            }
        }
        
        // All levels have green flags (or are not in the dictionary)
        if (debugMode)
        {
            Debug.Log("Win condition met: All levels have green flags");
        }
        return true;
    }
    
    /// <summary>
    /// Reset all flags (e.g., when starting a new game)
    /// </summary>
    public void ResetFlags()
    {
        levelFlags.Clear();
        
        if (debugMode)
        {
            Debug.Log("All level flags reset");
        }
    }
    
    /// <summary>
    /// Check the flag state for a specific level
    /// </summary>
    public bool GetLevelFlag(string levelName)
    {
        if (levelFlags.TryGetValue(levelName, out bool isGreenFlag))
        {
            return isGreenFlag;
        }
        
        // Default to green if not set
        return true;
    }
}