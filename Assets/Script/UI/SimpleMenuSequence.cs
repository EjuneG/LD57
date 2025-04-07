using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class SimpleMenuSequence : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private VideoPlayer introVideo;
    [SerializeField] private GameObject eyeObject;
    [SerializeField] private GameObject buttonsContainer;

    [Header("Timing")]
    [SerializeField] private float delayAfterVideo = 0.5f;
    [SerializeField] private float eyeAnimationDuration = 1.0f;

    private void Start()
    {
        // Hide elements at start
        eyeObject.SetActive(false);
        buttonsContainer.SetActive(false);
        
        // Begin sequence
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // 1. Play intro video
        introVideo.Play();
        
        // Wait for video to complete
        while (introVideo.isPlaying)
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(delayAfterVideo);
        
        // 2. Show eye and let animation play
        eyeObject.SetActive(true);

        //Start music
        AudioManager.Instance.PlayBGM("BGM");
        // The animation should be set to play automatically when the object is activated
        
        // Wait for eye animation to complete
        yield return new WaitForSeconds(eyeAnimationDuration);
        
        // 3. Show buttons
        buttonsContainer.SetActive(true);
        // Button animations should be set to play automatically when activated
    }
}