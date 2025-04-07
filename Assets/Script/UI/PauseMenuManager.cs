using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour, ExtraControl.IUIActions
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private SettingPanel settingsPanel; // Can be a prefab or scene reference
    [SerializeField] private Transform uiParent; // Parent transform for instantiated UI elements
    
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Audio Settings")]
    [SerializeField] private string buttonClickSFX = "ButtonClick";
    
    private bool isPaused = false;
    private bool isSettingsOpen = false;
    private ExtraControl inputControls;
    private SettingPanel instantiatedSettingsPanel; // Reference to runtime-instantiated panel
    
    private void Awake()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        // If settings panel exists in scene, ensure it's hidden at start
        if (settingsPanel != null && settingsPanel.gameObject.scene.IsValid())
            settingsPanel.gameObject.SetActive(false);
            
        // Initialize input system
        inputControls = new ExtraControl();
        
        // Set UI parent if not assigned
        if (uiParent == null)
            uiParent = transform;
    }
    
    private void Start()
    {
        // Set up button event listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
            
        Debug.Log("PauseMenuManager initialized. Settings panel reference: " + (settingsPanel != null));
    }
    
    private void OnEnable()
    {
        // Register for input callbacks
        inputControls.UI.SetCallbacks(this);
        
        // Enable the input action map
        inputControls.UI.Enable();
    }
    
    private void OnDisable()
    {
        // Disable input actions when component is disabled
        inputControls.UI.Disable();
    }
    
    // Implement the IUIActions interface
    public void OnOpenPause(InputAction.CallbackContext context)
    {
        // Only respond to the "performed" phase to avoid double-triggering
        if (context.performed)
        {
            HandleEscapePress();
        }
    }
    
    private void HandleEscapePress()
    {
        Debug.Log("ESC key pressed");
        
        // If settings panel is open, close it and return to pause menu
        if (isSettingsOpen)
        {
            CloseSettingsPanel();
            pauseMenuPanel.SetActive(true);
            return;
        }
        
        // Toggle pause state
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }
    
    public void PauseGame()
    {
        // Set time scale to 0 to pause the game
        Time.timeScale = 0f;
        isPaused = true;
        
        // Show pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
            
        Debug.Log("Game paused");
    }
    
    public void ResumeGame()
    {
        // Play button click sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        
        // Set time scale back to 1 to resume the game
        Time.timeScale = 1f;
        isPaused = false;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        Debug.Log("Game resumed");
    }
    
    private void OpenSettings()
    {
        Debug.Log("Opening settings panel");
        
        // Play button click sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        
        // Hide pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        // Check if we need to instantiate the settings panel or use an existing one
        if (settingsPanel != null)
        {
            // Check if the panel is a prefab (not in the scene)
            if (!settingsPanel.gameObject.scene.IsValid())
            {
                // Instantiate the prefab if we haven't already
                if (instantiatedSettingsPanel == null)
                {
                    Debug.Log("Instantiating settings panel from prefab");
                    instantiatedSettingsPanel = Instantiate(settingsPanel, uiParent);
                }
                
                // Show the instantiated panel
                instantiatedSettingsPanel.gameObject.SetActive(true);
            }
            else
            {
                // Use the scene panel
                Debug.Log("Using scene settings panel");
                settingsPanel.gameObject.SetActive(true);
            }
            
            isSettingsOpen = true;
        }
        else
        {
            Debug.LogError("Settings panel reference is missing!");
        }
    }
    
    private void CloseSettingsPanel()
    {
        // Determine which panel to close
        if (instantiatedSettingsPanel != null && instantiatedSettingsPanel.gameObject.activeSelf)
        {
            instantiatedSettingsPanel.ClosePanel();
        }
        else if (settingsPanel != null && settingsPanel.gameObject.scene.IsValid())
        {
            settingsPanel.ClosePanel();
        }
        
        isSettingsOpen = false;
    }
    
    private void ExitGame()
    {
        // Play button click sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        
        // In editor, just restore time scale and log
        #if UNITY_EDITOR
            Debug.Log("Exit button pressed. Application would quit here in a build.");
            Time.timeScale = 1f; // Restore time scale for editor
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // In an actual build, quit the application
            Application.Quit();
        #endif
    }
    
    // Ensure time scale is restored if script is disabled while paused
    private void OnDestroy()
    {
        Time.timeScale = 1f;
        
        // Clean up instantiated settings panel if it exists
        if (instantiatedSettingsPanel != null)
        {
            Destroy(instantiatedSettingsPanel.gameObject);
        }
    }
}