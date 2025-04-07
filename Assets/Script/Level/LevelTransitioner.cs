using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles transitions between levels with visual effects applied directly to the display image
/// </summary>
public class LevelTransitioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Image displayImage; // Direct reference to the display image
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve;
    
    [Header("Tinted Transitions")]
    [SerializeField] private bool useTintedTransitions = true;
    [SerializeField] private Color successTintColor = new Color(0.0f, 0.5f, 0.0f, 1.0f); // Green tint
    [SerializeField] private Color failureTintColor = new Color(0.5f, 0.0f, 0.0f, 1.0f); // Red tint
    [SerializeField] private Color defaultTintColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // Black tint
    [SerializeField] private float minBrightness = 0.0f; // How dark the image gets (0 = completely black)
    
    [Header("Level Configurations")]
    [SerializeField] private LevelData[] availableLevels;
    
    [Header("Scene Transitions")]
    [SerializeField] private bool enableSceneTransitions = true;
    [SerializeField] private string victorySceneName = "Victory";
    [SerializeField] private string defeatSceneName = "Defeat";
    [SerializeField] private string finalTransitionTrigger = "FinalTransition";
    
    // Internal state
    private string pendingLevelName;
    private bool isTransitioning = false;
    private Color originalImageColor; // Store the original color to restore after transition
    
    private void OnEnable()
    {
        // Subscribe to level transition events
        GameEvents.OnLevelTransition += HandleLevelTransition;
        GameEvents.OnLevelEvent += HandleLevelEvent;
        
        // We'll subscribe to narration events in Start() to ensure NarrationManager is fully initialized
    }
    
    private void OnDisable()
    {
        // Stop all coroutines to prevent any pending transitions
        StopAllCoroutines();
        
        // Unsubscribe from events
        GameEvents.OnLevelTransition -= HandleLevelTransition;
        GameEvents.OnLevelEvent -= HandleLevelEvent;
        
        // Unsubscribe from narration events
        NarrationManager narrationManager = FindObjectOfType<NarrationManager>();
        if (narrationManager != null)
        {
            narrationManager.OnNarrationLineCompleted -= HandleNarrationLineCompleted;
            narrationManager.OnNarrationSetCompleted -= HandleNarrationSetCompleted;
        }
    }
    
    private void Start()
    {
        // Find components if not assigned
        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();
            
        // Store the original image color
        if (displayImage != null)
        {
            originalImageColor = displayImage.color;
            Debug.Log("LevelTransitioner: Found display image");
        }
        else
        {
            Debug.LogError("LevelTransitioner: DisplayImage not assigned!");
        }
        
        // Subscribe to narration end events for auto-transitions
        // We do this in Start to ensure NarrationManager is fully initialized
        StartCoroutine(SubscribeToNarrationEventsWithDelay());
    }
    
    private IEnumerator SubscribeToNarrationEventsWithDelay()
    {
        // Wait for a frame to ensure everything is initialized
        yield return null;
        
        NarrationManager narrationManager = FindObjectOfType<NarrationManager>();
        if (narrationManager != null)
        {
            narrationManager.OnNarrationLineCompleted += HandleNarrationLineCompleted;
            narrationManager.OnNarrationSetCompleted += HandleNarrationSetCompleted;
            Debug.Log("LevelTransitioner: Subscribed to narration events");
        }
        else
        {
            Debug.LogWarning("LevelTransitioner: Could not find NarrationManager to subscribe to events");
        }
    }
    
    /// <summary>
    /// Handle custom level events
    /// </summary>
    private void HandleLevelEvent(string eventId)
    {
        // Check if this is our final transition trigger
        if (eventId == finalTransitionTrigger && enableSceneTransitions)
        {
            // Simple check - if we're in a victory level, go to victory scene, otherwise defeat
            string currentLevel = FindActiveLevelName();
            
            // Check if the current level name contains "Win" to determine if it's a victory
            bool isVictory = currentLevel != null && currentLevel.Contains("Win");
            TransitionToEndingScene(isVictory);
        }
    }
    
    /// <summary>
    /// Get the current level name
    /// </summary>
    private string FindActiveLevelName()
    {
        // Try to find the level name from the level manager
        if (levelManager != null)
        {
            // Use reflection to get the private field
            var levelDataField = levelManager.GetType().GetField("levelData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (levelDataField != null)
            {
                LevelData currentLevelData = levelDataField.GetValue(levelManager) as LevelData;
                if (currentLevelData != null)
                {
                    return currentLevelData.levelName;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Handle a level transition event
    /// </summary>
    private void HandleLevelTransition(string levelName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning to a level, ignoring request");
            return;
        }
        
        // Find the level data
        LevelData nextLevel = FindLevelByName(levelName);
        if (nextLevel == null)
        {
            Debug.LogError($"Could not find level data for level: {levelName}");
            return;
        }
        
        Debug.Log($"Transitioning to level: {levelName}");
        pendingLevelName = levelName;
        StartCoroutine(TransitionToLevel(nextLevel));
    }
    
    /// <summary>
    /// Transition to the ending scene (victory or defeat)
    /// </summary>
    public void TransitionToEndingScene(bool isVictory)
    {
        if (!enableSceneTransitions)
        {
            Debug.LogWarning("Scene transitions are disabled!");
            return;
        }
        
        string targetScene = isVictory ? victorySceneName : defeatSceneName;
        Debug.Log($"Transitioning to {(isVictory ? "victory" : "defeat")} scene: {targetScene}");
        
        // Start the scene transition coroutine
        StartCoroutine(TransitionToScene(targetScene, isVictory));
    }
    
    /// <summary>
    /// Coroutine to handle scene transitions with fade effect
    /// </summary>
    private IEnumerator TransitionToScene(string sceneName, bool isVictory)
    {
        isTransitioning = true;
        
        // Determine which tint color to use
        Color tintColor = isVictory ? successTintColor : failureTintColor;
        
        // Fade out
        if (displayImage != null && useTintedTransitions)
        {
            yield return StartCoroutine(TintImageWithDarkening(tintColor, transitionTime / 2));
        }
        else
        {
            // Fallback to simple darkening if no display image or tinting disabled
            yield return new WaitForSeconds(transitionTime / 2);
        }
        
        // Check if the scene exists in the build settings
        if (SceneUtility.GetBuildIndexByScenePath(sceneName) < 0)
        {
            Debug.LogError($"Scene '{sceneName}' is not in the build settings! Make sure to add it.");
            isTransitioning = false;
            yield break;
        }
        
        // Load the new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Find level data by name from available levels
    /// </summary>
    private LevelData FindLevelByName(string levelName)
    {
        if (availableLevels == null || availableLevels.Length == 0)
        {
            Debug.LogWarning("No available levels configured in LevelTransitioner");
            return null;
        }
        
        foreach (var level in availableLevels)
        {
            if (level != null && level.levelName == levelName)
            {
                return level;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Transition coroutine - handles the tint effect and level loading
    /// </summary>
    private IEnumerator TransitionToLevel(LevelData nextLevel)
    {
        isTransitioning = true;
        
        // Play SFX
        AudioManager.Instance.PlaySFX("transition");
        // Determine which tint color to use based on the level's transitionColor property
        if (displayImage != null && useTintedTransitions && nextLevel != null)
        {
            Color tintColor = defaultTintColor;
            
            // Use the level's specified transition color
            switch (nextLevel.transitionColor)
            {
                case TransitionColorType.Green:
                    tintColor = successTintColor;
                    break;
                case TransitionColorType.Red:
                    tintColor = failureTintColor;
                    break;
                case TransitionColorType.Default:
                default:
                    tintColor = defaultTintColor;
                    break;
            }
            
            // Tint and darken the image
            yield return StartCoroutine(TintImageWithDarkening(tintColor, transitionTime / 2));
        }
        else
        {
            // Fallback to simple wait if no display image or tinting disabled
            yield return new WaitForSeconds(transitionTime / 2);
        }
        
        // Load the new level
        if (levelManager != null)
        {
            // Set the new level data
            SetLevelData(nextLevel);
            
            // Short pause to allow setup
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            Debug.LogError("LevelTransitioner: No LevelManager found!");
        }
        
        // Restore the original image color
        if (displayImage != null)
        {
            yield return StartCoroutine(RestoreImageColor(transitionTime / 2));
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Tint the display image with progressive darkening
    /// </summary>
    private IEnumerator TintImageWithDarkening(Color tintColor, float duration)
    {
        if (displayImage == null)
        {
            Debug.LogError("TintImageWithDarkening: No display image found!");
            yield break;
        }
        
        // Store initial color
        Color startColor = displayImage.color;
        float startTime = Time.time;
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Apply animation curve if available
            if (fadeCurve != null && fadeCurve.keys.Length > 0)
            {
                t = fadeCurve.Evaluate(t);
            }
            
            // Progressive darkening with tint
            // Calculate brightness factor - goes from 1 (full brightness) to minBrightness
            float brightnessFactor = Mathf.Lerp(1.0f, minBrightness, t);
            
            // Blend between original color and tinted dark color
            Color currentColor = new Color(
                Mathf.Lerp(startColor.r, tintColor.r * brightnessFactor, t),
                Mathf.Lerp(startColor.g, tintColor.g * brightnessFactor, t),
                Mathf.Lerp(startColor.b, tintColor.b * brightnessFactor, t),
                startColor.a // Maintain original alpha
            );
            
            // Apply the color
            displayImage.color = currentColor;
            
            yield return null;
        }
        
        // Ensure we reach the target color (tinted black)
        Color endColor = new Color(
            tintColor.r * minBrightness,
            tintColor.g * minBrightness,
            tintColor.b * minBrightness,
            startColor.a
        );
        
        displayImage.color = endColor;
    }
    
    /// <summary>
    /// Restore the display image to its original color
    /// </summary>
    private IEnumerator RestoreImageColor(float duration)
    {
        if (displayImage == null)
        {
            Debug.LogError("RestoreImageColor: No display image found!");
            yield break;
        }
        
        // Store current color
        Color startColor = displayImage.color;
        float startTime = Time.time;
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Apply animation curve if available
            if (fadeCurve != null && fadeCurve.keys.Length > 0)
            {
                t = fadeCurve.Evaluate(t);
            }
            
            // Blend from current color to original color
            Color currentColor = Color.Lerp(startColor, originalImageColor, t);
            
            // Apply the color
            displayImage.color = currentColor;
            
            yield return null;
        }
        
        // Ensure we reach the original color
        displayImage.color = originalImageColor;
    }
    
    /// <summary>
    /// Set the level data in the level manager through reflection to avoid modifying the original class
    /// </summary>
    private void SetLevelData(LevelData newLevelData)
    {
        // Using reflection to set the private field
        var levelDataField = levelManager.GetType().GetField("levelData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (levelDataField != null)
        {
            levelDataField.SetValue(levelManager, newLevelData);
            
            // Now load the level data
            levelManager.LoadLevelData();
        }
        else
        {
            Debug.LogError("LevelTransitioner: Could not access levelData field in LevelManager");
        }
    }
    
    /// <summary>
    /// Handle narration line completion for level transitions
    /// </summary>
    private void HandleNarrationLineCompleted(NarrationLine line)
    {
        // Only process the transition after the narration line has completed
        if (line != null && line.transitionAfterLine && !string.IsNullOrEmpty(line.transitionToLevel))
        {
            // Add a small delay to ensure narration is completely done
            StartCoroutine(DelayedTransition(line.transitionToLevel, 0.1f));
        }
    }
    
    /// <summary>
    /// Delay the transition slightly to ensure other processes complete first
    /// </summary>
    private IEnumerator DelayedTransition(string levelName, float delay)
    {
        yield return new WaitForSeconds(delay);
        GameEvents.TriggerOnLevelTransition(levelName);
    }
    
    /// <summary>
    /// Handle narration set completion for level transitions
    /// </summary>
    private void HandleNarrationSetCompleted(NarrationSet set)
    {
        if (set != null && set.transitionAfterSet && !string.IsNullOrEmpty(set.transitionToLevel))
        {
            // Add a small delay to ensure narration is completely done
            StartCoroutine(DelayedTransition(set.transitionToLevel, 0.1f));
        }
    }
    
    /// <summary>
    /// Public method to trigger a transition to a specific level
    /// </summary>
    public void TransitionToLevel(string levelName)
    {
        GameEvents.TriggerOnLevelTransition(levelName);
    }
}