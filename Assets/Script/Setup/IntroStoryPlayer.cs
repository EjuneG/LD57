using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class IntroStoryPlayer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image storyImage;
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private GameObject textBox;
    
    [Header("Typewriter Settings")]
    [SerializeField] private float baseTypeSpeed = 0.05f;
    [SerializeField] private float speedVariation = 0.03f;
    [SerializeField] private float pauseOnPunctuation = 0.2f;
    
    [Header("Transition Settings")]
    [SerializeField] private float imageFadeDuration = 0.5f;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    
    [Header("Story Settings")]
    [SerializeField] private StorySlide[] storySlides;
    [SerializeField] private bool autoAdvance = true;
    [SerializeField] private string nextSceneName;
    
    private int currentSlideIndex = 0;
    private bool isTyping = false;
    private bool canAdvance = false;
    private Coroutine typingCoroutine;
    
    private void Awake()
    {
        // If no canvas group assigned, try to get or add one
        if (imageCanvasGroup == null && storyImage != null)
        {
            imageCanvasGroup = storyImage.GetComponent<CanvasGroup>();
            if (imageCanvasGroup == null)
            {
                imageCanvasGroup = storyImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
    
    private void Start()
    {
        if (storySlides != null && storySlides.Length > 0)
        {
            StartStory();
        }
        else
        {
            Debug.LogError("No story slides assigned!");
        }
    }
    
    private void Update()
    {
        // Check for click/tap to advance
        if (Input.GetMouseButtonDown(0) && canAdvance)
        {
            AdvanceStory();
        }
    }
    
    public void StartStory()
    {
        currentSlideIndex = 0;
        ShowCurrentSlide();
    }
    
    public void AdvanceStory()
    {
        // If currently typing, show full text immediately
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            if (textDisplay != null && currentSlideIndex < storySlides.Length)
            {
                textDisplay.text = storySlides[currentSlideIndex].text;
            }
            isTyping = false;
            canAdvance = true;
            return;
        }
        
        // Check if we have more slides
        if (currentSlideIndex >= storySlides.Length - 1)
        {
            // End of story
            EndStory();
            return;
        }
        
        // Get current and next slide
        StorySlide currentSlide = storySlides[currentSlideIndex];
        currentSlideIndex++;
        StorySlide nextSlide = storySlides[currentSlideIndex];
        
        // Check if image changes
        if (currentSlide.image != nextSlide.image)
        {
            // Image changes, do transition
            StartCoroutine(TransitionToNextSlide(nextSlide));
        }
        else
        {
            // Image doesn't change, just update text
            ShowCurrentSlide();
        }
    }
    
    private void ShowCurrentSlide()
    {
        if (currentSlideIndex >= storySlides.Length) return;
        
        StorySlide slide = storySlides[currentSlideIndex];
        
        // Show text box
        if (textBox != null)
        {
            textBox.SetActive(true);
        }
        
        // Update image
        if (storyImage != null && slide.image != null)
        {
            storyImage.sprite = slide.image;
        }
        
        // Start typing
        textDisplay.color = slide.textColor;
        typingCoroutine = StartCoroutine(TypeText(slide.text));
    }
    
    private IEnumerator TransitionToNextSlide(StorySlide nextSlide)
    {
        // Fade out current image
        if (imageCanvasGroup != null)
        {
            float startTime = Time.time;
            
            // Fade out
            while (Time.time < startTime + imageFadeDuration * 0.5f)
            {
                float t = (Time.time - startTime) / (imageFadeDuration * 0.5f);
                imageCanvasGroup.alpha = 1 - t;
                yield return null;
            }
            imageCanvasGroup.alpha = 0; // Ensure alpha is exactly 0
            
            // Change image
            storyImage.sprite = nextSlide.image;
            
            // Fade in
            startTime = Time.time;
            while (Time.time < startTime + imageFadeDuration * 0.5f)
            {
                float t = (Time.time - startTime) / (imageFadeDuration * 0.5f);
                imageCanvasGroup.alpha = t;
                yield return null;
            }
            
            imageCanvasGroup.alpha = 1;
        }
        else
        {
            // Simple swap if no canvas group
            storyImage.sprite = nextSlide.image;
        }
        
        // Start typing the new text
        textDisplay.color = nextSlide.textColor;
        typingCoroutine = StartCoroutine(TypeText(nextSlide.text));
    }
    
    private void EndStory()
    {
        StartCoroutine(EndStorySequence());
    }
    
    private IEnumerator EndStorySequence()
    {
        // Optional fade out
        if (imageCanvasGroup != null)
        {
            float startTime = Time.time;
            while (Time.time < startTime + imageFadeDuration)
            {
                float t = (Time.time - startTime) / imageFadeDuration;
                imageCanvasGroup.alpha = 1 - t;
                yield return null;
            }
            imageCanvasGroup.alpha = 0; // Ensure alpha is exactly 0
        }
        
        // Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
    
    /// <summary>
    /// Typewriter effect coroutine with variable typing speed
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        canAdvance = false;
        
        if (textDisplay != null)
        {
            textDisplay.text = "";
            
            for (int i = 0; i < text.Length; i++)
            {
                // Add one character at a time
                textDisplay.text += text[i];
                
                // Variable timing for typewriter effect
                float delay = baseTypeSpeed;
                
                // Add variation to make it feel more organic
                delay += UnityEngine.Random.Range(-speedVariation, speedVariation);
                
                // Longer pauses on punctuation
                if (text[i] == '.' || text[i] == ',' || text[i] == '?' || text[i] == '!')
                {
                    delay += pauseOnPunctuation;
                }
                
                yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
            }
        }
        
        isTyping = false;
        canAdvance = true;
        
        // Auto-advance after a delay if enabled
        if (autoAdvance)
        {
            StorySlide currentSlide = storySlides[currentSlideIndex];
            yield return new WaitForSeconds(currentSlide.displayDuration);
            AdvanceStory();
        }
    }
}