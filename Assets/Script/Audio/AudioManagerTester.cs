using UnityEngine;
using UnityEngine.UI;

public class AudioManagerTester : MonoBehaviour
{
    [Header("BGM Controls")]
    [SerializeField] private Button playBgmButton;
    [SerializeField] private Button pauseBgmButton;
    [SerializeField] private Button resumeBgmButton;
    [SerializeField] private Button nextBgmButton;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider bgmPitchSlider;
    
    [Header("SFX Controls")]
    [SerializeField] private Button[] sfxButtons;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Master Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    
    private int currentBgmIndex = 0;
    
    private void Start()
    {
        // Set up BGM controls
        if (playBgmButton != null)
            playBgmButton.onClick.AddListener(() => AudioManager.Instance.PlayBGM(currentBgmIndex));
            
        if (pauseBgmButton != null)
            pauseBgmButton.onClick.AddListener(() => AudioManager.Instance.PauseBGM());
            
        if (resumeBgmButton != null)
            resumeBgmButton.onClick.AddListener(() => AudioManager.Instance.ResumeBGM());
            
        if (nextBgmButton != null)
            nextBgmButton.onClick.AddListener(PlayNextBGM);
            
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetBGMVolume(value));
            bgmVolumeSlider.value = 0.7f; // Default value
        }
            
        if (bgmPitchSlider != null)
        {
            bgmPitchSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetBGMPitch(value));
            bgmPitchSlider.value = 1f; // Default value
        }
        
        // Set up SFX controls
        for (int i = 0; i < sfxButtons.Length; i++)
        {
            int index = i; // Need to capture the index in a local variable
            if (sfxButtons[i] != null)
                sfxButtons[i].onClick.AddListener(() => AudioManager.Instance.PlaySFX(index));
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetSFXVolume(value));
            sfxVolumeSlider.value = 1f; // Default value
        }
        
        // Set up master volume control
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetMasterVolume(value));
            masterVolumeSlider.value = 1f; // Default value
        }
    }
    
    private void PlayNextBGM()
    {
        // Get how many BGM clips we have (non-null ones)
        int bgmCount = 0;
        var audioManager = AudioManager.Instance;
        
        // This is just a test function to simulate cycling through BGMs
        // In a real implementation, you would use the actual count from the AudioManager
        bgmCount = 3; // Assuming we have 3 BGMs for testing
        
        if (bgmCount > 0)
        {
            currentBgmIndex = (currentBgmIndex + 1) % bgmCount;
            audioManager.PlayBGM(currentBgmIndex);
        }
    }
}