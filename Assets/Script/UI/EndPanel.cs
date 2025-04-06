using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    void Start()
    {
        // Setup button actions
        if (continueButton != null)
            continueButton.onClick.AddListener(RestartLoop);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    public void RestartLoop()
    {
        // Play click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
        
        // Load the gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

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
