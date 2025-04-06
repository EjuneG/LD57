using UnityEngine;

public class NarrationTrigger : MonoBehaviour
{
    [SerializeField] private NarrationLine singleLine;
    [SerializeField] private NarrationSet narrationSet;
    [SerializeField] private bool triggerOnStart = false;
    [SerializeField] private bool triggerOnEnter = true;
    
    private NarrationManager narrationManager;
    
    private void Start()
    {
        // Find the narration manager in the scene
        narrationManager = FindObjectOfType<NarrationManager>();
        
        if (narrationManager == null)
        {
            Debug.LogError("NarrationManager not found in scene!");
            return;
        }
        
        if (triggerOnStart)
        {
            TriggerNarration();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            TriggerNarration();
        }
    }
    
    public void TriggerNarration()
    {
        if (narrationManager == null) return;
        
        if (narrationSet != null)
        {
            narrationManager.PlayNarrationSet(narrationSet);
        }
        else if (singleLine != null)
        {
            narrationManager.PlayLine(singleLine);
        }
    }
}