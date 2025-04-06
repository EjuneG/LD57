using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class FrameSensitiveButton : MonoBehaviour
{
    [Serializable]
    public class FrameRange
    {
        public int startFrame;
        public int endFrame;
    }
    
    [Header("Identification")]
    [SerializeField] private string objectId = ""; // Used to identify this object
    [SerializeField] private string displayName = ""; // For display purposes
    
    [Header("Frame Settings")]
    [SerializeField] private FrameRange[] activeFrameRanges;
    [SerializeField] private FOVImageController fovController;
    
    [Header("Visualization (Optional)")]
    [SerializeField] private bool showOutline = false;
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineThickness = 2f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;
    
    // Properties
    public string ObjectId => objectId;
    
    // Internal state
    private Button button;
    private Image buttonImage;
    private Outline outline;
    private int currentFrame = -1;
    private bool isCurrentlyActive = false;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Get components
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        // Configure visual elements
        ConfigureVisuals();
        
        // Start with button disabled but don't deactivate the GameObject
        button.interactable = false;
        
        // Listen for button click
        button.onClick.AddListener(OnButtonClicked);
        
        isInitialized = true;
    }
    
    private void ConfigureVisuals()
    {
        // Set up image if present
        if (buttonImage != null)
        {
            // Make the button mostly transparent but clickable
            Color transparentColor = buttonImage.color;
            transparentColor.a = 0.01f; // Almost invisible but still registers clicks
            buttonImage.color = transparentColor;
            
            // Add outline component if visualizing is enabled
            if (showOutline)
            {
                outline = GetComponent<Outline>();
                if (outline == null)
                {
                    outline = gameObject.AddComponent<Outline>();
                }
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(outlineThickness, outlineThickness);
                outline.enabled = false; // Start disabled
            }
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to frame change events
        GameEvents.OnFrameChanged += OnFrameChanged;
        
        // Initialize with current frame if available
        if (isInitialized && fovController != null)
        {
            OnFrameChanged(fovController.CurrentFrameIndex);
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnFrameChanged -= OnFrameChanged;
    }
    
    private void Start()
    {
        // Find FOV controller if not assigned
        if (fovController == null)
            fovController = FindObjectOfType<FOVImageController>();
            
        // Initial state check
        if (fovController != null)
        {
            OnFrameChanged(fovController.CurrentFrameIndex);
        }
    }
    
    private void OnDestroy()
    {
        // Remove button click listener
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    private void OnFrameChanged(int frameIndex)
    {
        // Store current frame
        currentFrame = frameIndex;
        
        // Update interactivity based on frame
        UpdateInteractivity();
    }
    
    private void UpdateInteractivity()
    {
        bool shouldBeActive = IsInActiveFrameRange();
        
        // Only update if state changed
        if (shouldBeActive != isCurrentlyActive)
        {
            isCurrentlyActive = shouldBeActive;
            
            // Update button interactability
            button.interactable = isCurrentlyActive;
            
            // Update outline visibility if using outlines
            if (outline != null)
                outline.enabled = isCurrentlyActive && showOutline;
        }
    }
    
    // Check if current frame is within any defined active range
    private bool IsInActiveFrameRange()
    {
        if (activeFrameRanges == null || activeFrameRanges.Length == 0)
            return false;
        
        // If we don't have a valid frame yet
        if (currentFrame < 0)
        {
            // Try to get it from the controller if available
            if (fovController != null)
            {
                currentFrame = fovController.CurrentFrameIndex;
            }
            else
            {
                return false; // Can't determine activity without a frame
            }
        }
        
        // Check all ranges
        foreach (var range in activeFrameRanges)
        {
            if (currentFrame >= range.startFrame && currentFrame <= range.endFrame)
                return true;
        }
        
        return false;
    }
    
    // Handle button click
    private void OnButtonClicked()
    {
        Debug.Log("Clicked");
        // Trigger local event
        onInteract?.Invoke();
        
        // Trigger global event
        if (!string.IsNullOrEmpty(objectId))
        {
            GameEvents.TriggerOnObjectInteracted(objectId);
        }
    }
    
    // Add a listener to the button
    public void AddListener(UnityAction action)
    {
        onInteract.AddListener(action);
    }
    
    // Clear all listeners
    public void ClearListeners()
    {
        onInteract.RemoveAllListeners();
    }
    
    // Set a single frame range programmatically
    public void SetFrameRange(int startFrame, int endFrame)
    {
        activeFrameRanges = new FrameRange[] { 
            new FrameRange { startFrame = startFrame, endFrame = endFrame } 
        };
        
        // Update interactivity state after changing range
        UpdateInteractivity();
    }
    
    // Set multiple frame ranges programmatically
    public void SetFrameRanges(FrameRange[] ranges)
    {
        activeFrameRanges = ranges;
        
        // Update interactivity state after changing ranges
        UpdateInteractivity();
    }
    
    // Set the object ID
    public void SetObjectId(string id)
    {
        objectId = id;
    }
    
    // Manually force update (useful after configuration changes)
    public void ForceUpdate()
    {
        if (fovController != null)
        {
            OnFrameChanged(fovController.CurrentFrameIndex);
        }
    }
}