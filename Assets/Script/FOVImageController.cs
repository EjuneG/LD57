using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FOVImageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image displayImage; // The UI Image component to display the FOV frames
    private Sprite[] fovFrames; // Array of all FOV frame sprites
    [SerializeField] private string resourcePath = "FOVFrames"; // Path to folder containing sprites
    
    [Header("Settings")]
    [SerializeField] private float distancePerFrame = 20f; // Pixels of vertical movement needed to change one frame
    [SerializeField] private bool invertDrag = false; // Whether to invert the drag direction
    [SerializeField] private bool wrapAround = true; // Whether to loop from end to beginning and vice versa
    [SerializeField] private bool hideCursorWhileDragging = true; // Whether to hide the cursor while dragging
    
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
        
        // Set frame index within valid range
        currentFrameIndex = Mathf.Clamp(startFrameIndex, 0, totalFrames - 1);
        
        // Update display
        if (displayImage != null)
        {
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
            
            // Hide cursor and lock it at the center of the screen
            if (hideCursorWhileDragging)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; // Lock cursor in place
            }
        }
        
        // Check for mouse button release
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
            
            // Restore cursor visibility and position
            if (hideCursorWhileDragging)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; // Return to normal cursor behavior
                
                UnityEngine.InputSystem.Mouse.current.WarpCursorPosition(mouseDragStartPosition);
            }
        }
        
        // Handle dragging using raw mouse delta
        if (isDragging)
        {
            // Get the mouse delta directly
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            float verticalDelta = mouseDelta.y;
            
            // Apply inversion if needed
            if (invertDrag)
            {
                verticalDelta = -verticalDelta;
            }
            
            // Scale the delta to make it more responsive to small movements
            verticalDelta *= 10f; // Adjust this multiplier as needed
            
            // Accumulate the drag distance
            accumulatedDragDistance += verticalDelta;
            
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
    
    private void OnDisable()
    {
        // Make sure cursor is visible and unlocked if script is disabled while dragging
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}