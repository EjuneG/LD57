using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    
    [Header("Audio Settings")]
    [SerializeField] private string menuMusicName = "MenuMusic";
    
    private void Start()
    {
        // Initialize the menu music if AudioManager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(menuMusicName);
        }
        
        // Setup button actions
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Ensure settings panel is initially closed
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    // Start the game by transitioning to the gameplay scene
    public void StartGame()
    {
        // Play click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
        
        // Load the gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }
    
    // Open the settings panel
    public void OpenSettings()
    {
        // Play click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
        
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }
    
    // Quit the game
    public void QuitGame()
    {
        // Play click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
        
        // Quit application (only works in built game, not in editor)
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}