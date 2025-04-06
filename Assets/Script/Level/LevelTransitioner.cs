using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Added for scene transitions

/// <summary>
/// Handles transitions between levels with visual effects and proper state management
/// </summary>
public class LevelTransitioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Image transitionPanel;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve;
    [SerializeField] private bool useScaleEffect = false;
    
    [Header("Level Configurations")]
    [SerializeField] private LevelData[] availableLevels;
    
    [Header("Win Condition")]
    [SerializeField] private WinConditionManager winConditionManager;
    [SerializeField] private string finalWinLevel = "Win";
    [SerializeField] private string finalLossLevel = "Loss";
    
    [Header("Scene Transitions")]
    [SerializeField] private bool enableSceneTransitions = true;
    [SerializeField] private string victorySceneName = "Victory";
    [SerializeField] private string defeatSceneName = "Defeat";
    [SerializeField] private string finalTransitionTrigger = "FinalTransition";
    
    // Internal state
    private string pendingLevelName;
    private bool isTransitioning = false;
    
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
        
        if (transitionPanel == null)
        {
            // Create transition panel if it doesn't exist
            CreateTransitionPanel();
        }
        
        // Find win condition manager if not assigned
        if (winConditionManager == null)
            winConditionManager = FindObjectOfType<WinConditionManager>();
        
        // Start with panel invisible
        if (transitionPanel != null)
        {
            SetPanelAlpha(0);
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
            // Determine if we're in win or loss state
            string currentLevel = winConditionManager?.currentLevel ?? "";
            
            if (currentLevel == finalWinLevel)
            {
                // We're in the win level, transition to victory scene
                TransitionToEndingScene(true);
            }
            else if (currentLevel == finalLossLevel)
            {
                // We're in the loss level, transition to defeat scene
                TransitionToEndingScene(false);
            }
            else
            {
                Debug.LogWarning($"Final transition triggered from unexpected level: {currentLevel}");
            }
        }
    }
    
    /// <summary>
    /// Create a transition panel if one doesn't exist
    /// </summary>
    private void CreateTransitionPanel()
    {
        // Create a canvas if needed
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Very front
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create panel
        GameObject panelObj = new GameObject("TransitionPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        // Configure panel to cover the screen
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Add image component
        transitionPanel = panelObj.AddComponent<Image>();
        transitionPanel.color = new Color(0, 0, 0, 0); // Transparent black
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
        
        // Check if we should override the requested level based on win condition flags
        string targetLevelName = levelName;
        
        // Special case for final level check (win vs loss)
        if (winConditionManager != null && (levelName == finalWinLevel || levelName == finalLossLevel))
        {
            // Check overall win condition to decide which final level to show
            targetLevelName = winConditionManager.CheckWinCondition() ? finalWinLevel : finalLossLevel;
            Debug.Log($"Final level check: Using {targetLevelName} based on win condition");
        }
        
        // Find the level data
        LevelData nextLevel = FindLevelByName(targetLevelName);
        if (nextLevel == null)
        {
            Debug.LogError($"Could not find level data for level: {targetLevelName}");
            return;
        }
        
        Debug.Log($"Transitioning to level: {targetLevelName}");
        pendingLevelName = targetLevelName;
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
        StartCoroutine(TransitionToScene(targetScene));
    }
    
    /// <summary>
    /// Coroutine to handle scene transitions with fade effect
    /// </summary>
    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;
        
        // Fade out
        yield return StartCoroutine(FadePanel(0, 1, transitionTime / 2));
        
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
        
        // After the scene is loaded, fade back in
        // Note: This assumes that there's a LevelTransitioner in the new scene that will handle the fade-in
        // If not, you might need a different approach to fade in the new scene
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Transition to the next level based on the current level's flag
    /// This should be called when we want to progress to the next level using branching logic
    /// </summary>
    public void TransitionToLevelBasedOnFlag(string currentLevelName)
    {
        if (winConditionManager == null)
        {
            // If no win condition manager, just do a regular transition to the next level
            Debug.LogWarning("No WinConditionManager found for branching, using default next level");
            GameEvents.TriggerOnLevelTransition(currentLevelName + "+1");
            return;
        }
        
        // Get the next level based on the current level's flag
        string nextLevelName = winConditionManager.GetNextLevel(currentLevelName);
        if (string.IsNullOrEmpty(nextLevelName))
        {
            Debug.LogError($"No next level defined for {currentLevelName}");
            return;
        }
        
        Debug.Log($"Branching from {currentLevelName} to {nextLevelName} based on flag");
        
        // Trigger the transition to the determined next level
        GameEvents.TriggerOnLevelTransition(nextLevelName);
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
    /// Transition coroutine - handles the fade effect and level loading
    /// </summary>
    private IEnumerator TransitionToLevel(LevelData nextLevel)
    {
        isTransitioning = true;
        
        // Fade out
        yield return StartCoroutine(FadePanel(0, 1, transitionTime / 2));
        
        // Load the new level
        if (levelManager != null)
        {
            // Set the new level data
            SetLevelData(nextLevel);
            
            // Note: LevelManager.LoadLevelData already handles resetting to frame 0
            
            // Short pause to allow setup
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            Debug.LogError("LevelTransitioner: No LevelManager found!");
        }
        
        // Fade back in
        yield return StartCoroutine(FadePanel(1, 0, transitionTime / 2));
        
        isTransitioning = false;
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
    /// Fade panel from startAlpha to endAlpha over duration
    /// </summary>
    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
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
            
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            SetPanelAlpha(alpha);
            
            yield return null;
        }
        
        // Ensure we reach the target alpha
        SetPanelAlpha(endAlpha);
    }
    
    /// <summary>
    /// Set the panel's alpha value
    /// </summary>
    private void SetPanelAlpha(float alpha)
    {
        if (transitionPanel == null) return;
        
        Color color = transitionPanel.color;
        color.a = alpha;
        transitionPanel.color = color;
        
        // Apply scale effect if enabled
        if (useScaleEffect)
        {
            float scale = 1.0f + (0.1f * alpha);
            transitionPanel.transform.localScale = new Vector3(scale, scale, 1);
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