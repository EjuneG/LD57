using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple click sound manager that plays a sound on left click
/// unless the click is over a UI element.
/// </summary>
public class ClickSoundManager : MonoBehaviour
{
    // Singleton instance
    public static ClickSoundManager Instance { get; private set; }

    [Header("Click Sound Settings")]
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.5f;
    [Range(0.5f, 1.5f)]
    [SerializeField] private float clickPitch = 1f;
    [SerializeField] private float clickPitchVariation = 0.05f;
    [SerializeField] private float clickCooldown = 0.1f; // Prevents sound spam from rapid clicks

    // Private variables
    private AudioSource audioSource;
    private bool canPlayClick = true;
    private float clickTimer = 0f;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        // Update click cooldown timer
        if (!canPlayClick)
        {
            clickTimer += Time.deltaTime;
            if (clickTimer >= clickCooldown)
            {
                canPlayClick = true;
                clickTimer = 0f;
            }
        }

        // Check for mouse click
        if (Input.GetMouseButtonDown(0) && canPlayClick)
        {
            // Check if mouse is over UI element (don't play sound in that case)
            if (!IsPointerOverUIElement())
            {
                PlayClickSound();
            }
        }
    }

    /// <summary>
    /// Checks if the pointer is over a UI element
    /// </summary>
    private bool IsPointerOverUIElement()
    {
        // Check UI using EventSystem
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Play the click sound with slight pitch variation
    /// </summary>
    private void PlayClickSound()
    {
        if (clickSound == null || audioSource == null) return;

        // Apply random pitch variation
        float randomPitch = clickPitch + Random.Range(-clickPitchVariation, clickPitchVariation);
        audioSource.pitch = randomPitch;
        
        // Play the sound
        audioSource.PlayOneShot(clickSound, clickVolume);
        
        // Start cooldown
        canPlayClick = false;
        clickTimer = 0f;
    }
}