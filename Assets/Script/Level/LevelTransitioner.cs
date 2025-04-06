using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    
    // Internal state
    private string pendingLevelName;
    private bool isTransitioning = false;
    
    private void OnEnable()
    {
        // Subscribe to level transition events
        GameEvents.OnLevelTransition += HandleLevelTransition;
        
        // We'll subscribe to narration events in Start() to ensure NarrationManager is fully initialized
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
    
    private void OnDisable()
    {
        // Stop all coroutines to prevent any pending transitions
        StopAllCoroutines();
        
        // Unsubscribe from events
        GameEvents.OnLevelTransition -= HandleLevelTransition;
        
        // Unsubscribe from narration events
        NarrationManager narrationManager = FindObjectOfType<NarrationManager>();
        if (narrationManager != null)
        {
            narrationManager.OnNarrationLineCompleted -= HandleNarrationLineCompleted;
            narrationManager.OnNarrationSetCompleted -= HandleNarrationSetCompleted;
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
        
        // Find the level data
        LevelData nextLevel = FindLevelByName(levelName);
        if (nextLevel == null)
        {
            Debug.LogError($"Could not find level data for level: {levelName}");
            return;
        }
        
        pendingLevelName = levelName;
        StartCoroutine(TransitionToLevel(nextLevel));
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