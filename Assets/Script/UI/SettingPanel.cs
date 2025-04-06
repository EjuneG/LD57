using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingPanel : MonoBehaviour
{
    [Header("Panel Controls")]
    [SerializeField] private Button closeButton;
    
    [Header("Volume Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    
    [Header("Volume Label (Optional)")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    
    [Header("Audio Settings")]
    [SerializeField] private string buttonClickSFX = "ButtonClick";
    
    private void OnEnable()
    {
        // Initialize slider to current value from AudioManager
        InitializeSlider();
    }
    
    private void Start()
    {
        // Setup button actions
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
            
        // Setup slider callback
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(_ => UpdateVolumeLabel());
        }
        
        // Initialize volume label
        UpdateVolumeLabel();
    }
    
    // Close the settings panel
    public void ClosePanel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
        
        // Save preferences
        SavePreferences();
        
        // Hide the panel
        gameObject.SetActive(false);
    }
    
    // Initialize slider based on current AudioManager setting
    private void InitializeSlider()
    {
        if (AudioManager.Instance == null) return;
        
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
    }
    
    // Update volume text label with current percentage
    private void UpdateVolumeLabel()
    {
        if (masterVolumeLabel != null && masterVolumeSlider != null)
            masterVolumeLabel.text = $"Master Volume: {Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
    }
    
    // Save user preferences
    private void SavePreferences()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            
        PlayerPrefs.Save();
    }
    
    // Volume change handler
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }
}