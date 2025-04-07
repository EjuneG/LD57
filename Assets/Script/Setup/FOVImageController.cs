using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FOVImageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image displayImage; // The UI Image component to display the FOV frames
    private Sprite[] fovFrames; // Array of all FOV frame sprites
    [SerializeField] private string resourcePath = "FOVFrames"; // Path to folder containing sprites
    
    [Header("Settings")]
    [SerializeField] private float distancePerFrame = 20f; // Pixels of movement needed to change one frame
    [SerializeField] private DragDirection dragDirection = DragDirection.Up; // Direction to drag to move forward
    [SerializeField] private bool wrapAround = true; // Whether to loop from end to beginning and vice versa
    [SerializeField] private bool hideCursorWhileDragging = true; // Whether to hide the cursor while dragging
    
    [Header("Sensitivity Settings")]
    [SerializeField] [Range(0.1f, 3.0f)] private float mouseSensitivity = 1.0f; // Global sensitivity multiplier
    [SerializeField] private float maxFramesPerSecond = 10f; // Maximum number of frames that can be scrolled per second
    
    private int currentFrameIndex = 0;
    private int totalFrames;
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private float accumulatedDragDistance = 0f;
    private int startFrameIndex = 0;
    private Vector3 mouseDragStartPosition; // To store the initial mouse position
    
    // Public property to access current frame index
    public int CurrentFrameIndex => currentFrameIndex;
    public int TotalFrames => totalFrames;
    
    private void Start()
    {
        // Load all sprites from the specified resources folder
        LoadSpritesFromResources();
        
        totalFrames = fovFrames.Length;
        
        // Set initial frame
        if (totalFrames > 0 && displayImage != null)
        {
            displayImage.sprite = fovFrames[currentFrameIndex];
            
            // Trigger frame changed event
            GameEvents.TriggerOnFrameChanged(currentFrameIndex);
        }
        else
        {
            Debug.LogError("FOVImageController: Missing Image reference or no frames assigned!");
        }
    }
    
    private void LoadSpritesFromResources()
    {
        // Load all sprites from the specified resources folder
        fovFrames = Resources.LoadAll<Sprite>(resourcePath);
        
        // Check if we found any sprites
        if (fovFrames == null || fovFrames.Length == 0)
        {
            Debug.LogError($"FOVImageController: No sprites found at Resources/{resourcePath}");
            fovFrames = new Sprite[0];
        }
        else
        {
            Debug.Log($"FOVImageController: Loaded {fovFrames.Length} sprites from Resources/{resourcePath}");
            
            // Sort sprites by name to ensure correct order
            System.Array.Sort(fovFrames, (a, b) => a.name.CompareTo(b.name));
        }
    }
    
    // Updates the frame set to a new array of sprites
    public void UpdateFrameSet(Sprite[] newFrames, int startFrameIndex = 0)
    {
        if (newFrames == null || newFrames.Length == 0)
        {
            Debug.LogError("FOVImageController: Attempted to update with empty frame set!");
            return;
        }
        
        fovFrames = newFrames;
        totalFrames = fovFrames.Length;
        
        // Set frame index to the requested index (usually 0 for level transitions)
        currentFrameIndex = Mathf.Clamp(startFrameIndex, 0, totalFrames - 1);
        
        // Update display
        if (displayImage != null)
        {
            displayImage.sprite = fovFrames[currentFrameIndex];
            
            // Trigger frame changed event
            GameEvents.TriggerOnFrameChanged(currentFrameIndex);
        }
    }
    
    // Reset to the first frame
    public void ResetToFirstFrame()
    {
        if (fovFrames != null && fovFrames.Length > 0 && displayImage != null)
        {
            currentFrameIndex = 0;
            displayImage.sprite = fovFrames[currentFrameIndex];
            
            // Trigger frame changed event
            GameEvents.TriggerOnFrameChanged(currentFrameIndex);
        }
    }
    
    private void Update()
    {
        // Check for mouse button press
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
            startFrameIndex = currentFrameIndex;
            accumulatedDragDistance = 0f;
            mouseDragStartPosition = Input.mousePosition; // Record starting position
            
            // Hide cursor but don't lock it to the center immediately
            if (hideCursorWhileDragging)
            {
                Cursor.visible = false;
                // Don't lock the cursor immediately - this causes the flash
                // Cursor.lockState = CursorLockMode.Locked; 
            }
        }
        
        // Check for mouse button release
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
            
            // Restore cursor visibility and position
            if (hideCursorWhileDragging)
            {
                // First unlock cursor if it was locked
                Cursor.lockState = CursorLockMode.None;
                
                // Return cursor to where dragging started
                StartCoroutine(ResetCursorPosition());
            }
        }
        
        // Handle dragging using normalized mouse delta with rate limiting
        if (isDragging)
        {
            // Get the mouse delta
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            
            // Apply sensitivity multiplier
            mouseDelta *= mouseSensitivity;
            
            // Extract delta based on drag direction
            float movementDelta = 0f;
            
            switch (dragDirection)
            {
                case DragDirection.Up:
                    movementDelta = mouseDelta.y; // Original behavior
                    break;
                    
                case DragDirection.Down:
                    movementDelta = -mouseDelta.y; // Inverted Y
                    break;
                    
                case DragDirection.Left:
                    movementDelta = -mouseDelta.x; // Use X-axis, inverted
                    break;
                    
                case DragDirection.Right:
                    movementDelta = mouseDelta.x; // Use X-axis
                    break;
            }
            
            // Apply a fixed scaling factor for more consistent behavior
            movementDelta *= 10f;
            
            // Rate limit the maximum movement per second to create a more consistent experience
            float maxDeltaPerFrame = (maxFramesPerSecond * distancePerFrame) * Time.deltaTime;
            movementDelta = Mathf.Clamp(movementDelta, -maxDeltaPerFrame, maxDeltaPerFrame);
            
            // Accumulate the drag distance
            accumulatedDragDistance += movementDelta;
            
            // Calculate how many frames to move
            int frameChange = Mathf.FloorToInt(accumulatedDragDistance / distancePerFrame);
            
            if (frameChange != 0)
            {
                // Remove the consumed distance
                accumulatedDragDistance -= frameChange * distancePerFrame;
                
                // Calculate new frame index
                int newIndex = currentFrameIndex + frameChange;
                
                if (wrapAround)
                {
                    // Wrap around if we go past the limits
                    newIndex = (newIndex % totalFrames + totalFrames) % totalFrames;
                }
                else
                {
                    // Clamp to valid range
                    newIndex = Mathf.Clamp(newIndex, 0, totalFrames - 1);
                }
                
                // Update frame if needed
                if (newIndex != currentFrameIndex)
                {
                    currentFrameIndex = newIndex;
                    displayImage.sprite = fovFrames[currentFrameIndex];
                    
                    // Trigger frame changed event
                    GameEvents.TriggerOnFrameChanged(currentFrameIndex);
                }
            }
        }
    }
    
    // Coroutine to handle cursor reset with proper timing
    private IEnumerator ResetCursorPosition()
    {
        // Wait for the end of frame to ensure all rendering is done
        yield return new WaitForEndOfFrame();
        
        // Set cursor position back to start position
        UnityEngine.InputSystem.Mouse.current.WarpCursorPosition(mouseDragStartPosition);
        
        // Small delay before making cursor visible again to prevent flashing
        yield return new WaitForSeconds(0.01f);
        
        // Make cursor visible again
        Cursor.visible = true;
    }
    
    // Optional: Public method to set the frame index directly
    public void SetFrameIndex(int index)
    {
        if (index >= 0 && index < totalFrames)
        {
            currentFrameIndex = index;
            displayImage.sprite = fovFrames[currentFrameIndex];
            
            // Trigger frame changed event
            GameEvents.TriggerOnFrameChanged(currentFrameIndex);
        }
    }
    
    // Set the drag direction
    public void SetDragDirection(DragDirection direction)
    {
        dragDirection = direction;
    }
    
    private void OnDisable()
    {
        // Make sure cursor is visible and unlocked if script is disabled while dragging
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

[Serializable]
public enum DragDirection
{
    Up,     // Default: Drag up to move forward
    Down,   // Drag down to move forward
    Left,   // Drag left to move forward
    Right   // Drag right to move forward
}