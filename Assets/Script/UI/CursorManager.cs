using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor; // Default cursor texture
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero; // Hotspot for the cursor
    [Tooltip("If true, cursor will use ForceSoftware mode which preserves aspect ratio better")]
    [SerializeField] private bool useSoftwareCursor = true; // Use software cursor rendering
    
    private static CursorManager instance;
    
    private void Awake()
    {
        // Singleton pattern to ensure cursor persists across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set the cursor immediately upon awake
            SetupCursor();
        }
        else
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
        }
    }
    
    private void SetupCursor()
    {
        if (defaultCursor != null)
        {
            // Use ForceSoftware mode to better preserve cursor aspect ratio if selected
            CursorMode cursorMode = useSoftwareCursor ? CursorMode.ForceSoftware : CursorMode.Auto;
            Cursor.SetCursor(defaultCursor, cursorHotspot, cursorMode);
            Cursor.visible = true;
            
            Debug.Log("CursorManager: Custom cursor applied");
        }
        else
        {
            Debug.LogWarning("CursorManager: No cursor texture assigned");
        }
    }
    
    // Call this method if you need to reset the cursor for any reason
    public void ResetCursor()
    {
        SetupCursor();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // Re-apply cursor when application regains focus
        if (hasFocus)
        {
            SetupCursor();
        }
    }
    
    private void OnDisable()
    {
        // Restore default system cursor when disabled
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}