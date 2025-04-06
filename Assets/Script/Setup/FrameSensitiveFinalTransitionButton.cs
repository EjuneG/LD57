using UnityEngine;

/// <summary>
/// Specialized button that triggers transition to victory/defeat scenes
/// Inherits all frame sensitivity features from FrameSensitiveButton
/// </summary>
public class FrameSensitiveFinalTransitionButton : FrameSensitiveButton
{
    public enum TransitionType
    {
        Automatic,  // Determine based on current level (Win/Loss)
        Victory,    // Force victory ending
        Defeat      // Force defeat ending
    }
    
    [Header("Final Transition Settings")]
    [SerializeField] private string transitionTrigger = "FinalTransition";
    [SerializeField] private TransitionType transitionType = TransitionType.Automatic;
    [SerializeField] private bool showDebugLog = true;
    
    protected void Start()
    {
        // Call base Start to ensure everything is initialized
        base.Start();
        
        // Add our final transition listener
        AddListener(TriggerFinalTransition);
        
        if (showDebugLog)
        {
            Debug.Log($"Final transition button '{ObjectId}' initialized with type: {transitionType}");
        }
    }
    
    /// <summary>
    /// Method called when the button is clicked (after the base FrameSensitiveButton behavior)
    /// </summary>
    private void TriggerFinalTransition()
    {
        if (showDebugLog)
        {
            Debug.Log($"Final transition button '{ObjectId}' triggered with type: {transitionType}");
        }
        
        switch (transitionType)
        {
            case TransitionType.Automatic:
                // Use the standard event which will be handled by LevelTransitioner
                GameEvents.TriggerOnLevelEvent(transitionTrigger);
                break;
                
            case TransitionType.Victory:
                // Find the level transitioner and force victory transition
                LevelTransitioner transitioner = FindObjectOfType<LevelTransitioner>();
                if (transitioner != null)
                {
                    transitioner.TransitionToEndingScene(true);
                }
                else
                {
                    Debug.LogError("Cannot find LevelTransitioner to transition to victory scene");
                }
                break;
                
            case TransitionType.Defeat:
                // Find the level transitioner and force defeat transition
                LevelTransitioner defeatTransitioner = FindObjectOfType<LevelTransitioner>();
                if (defeatTransitioner != null)
                {
                    defeatTransitioner.TransitionToEndingScene(false);
                }
                else
                {
                    Debug.LogError("Cannot find LevelTransitioner to transition to defeat scene");
                }
                break;
        }
    }
}