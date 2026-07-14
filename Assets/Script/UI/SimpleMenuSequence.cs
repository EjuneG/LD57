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

    private bool videoFinished = false;

    private void OnVideoEnd(VideoPlayer vp)
    {
        videoFinished = true;
    }

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
        yield return StartCoroutine(PlayIntroVideo());

        yield return new WaitForSeconds(delayAfterVideo);
        
        // 2. Show eye and let animation play
        eyeObject.SetActive(true);

        // Let the eye render before starting music: on WebGL, loading/decoding the BGM
        // can stall the main thread, and it must stall on a visible frame, not on black.
        yield return null;

        //Start music
        AudioManager.Instance.PlayBGM("BGM");
        // The animation should be set to play automatically when the object is activated
        
        // Wait for eye animation to complete
        yield return new WaitForSeconds(eyeAnimationDuration);
        
        // 3. Show buttons
        buttonsContainer.SetActive(true);
        // Button animations should be set to play automatically when activated
    }

    /// <summary>
    /// Plays the intro video and returns the moment it finishes.
    /// On WebGL the embedded VideoClip renders as a black screen, so we stream the
    /// same file from StreamingAssets by URL (H.264 mp4 plays natively in browsers).
    /// End-of-playback is detected via the loopPointReached event rather than polling
    /// isPlaying — on WebGL isPlaying can stay true after the last frame, which would
    /// otherwise pad a few seconds of black onto the end. A safety cap still guarantees
    /// the sequence never hangs on black if the video fails to load or fire its event.
    /// </summary>
    private IEnumerator PlayIntroVideo()
    {
        if (introVideo == null)
        {
            yield break;
        }

        // Stop any auto-started (Play On Awake) playback before we reconfigure the source.
        introVideo.Stop();
        introVideo.isLooping = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Browsers can't decode an embedded VideoClip reliably — point at the real file instead.
        introVideo.source = VideoSource.Url;
        introVideo.url = System.IO.Path.Combine(Application.streamingAssetsPath, "intro_text.mp4");
#endif

        videoFinished = false;
        introVideo.loopPointReached += OnVideoEnd;

        // Prepare, capped so a stalled/failed load can't freeze the intro.
        introVideo.Prepare();
        float elapsed = 0f;
        while (!introVideo.isPrepared && elapsed < 8f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!introVideo.isPrepared)
        {
            Debug.LogWarning("[SimpleMenuSequence] Intro video failed to prepare in time; skipping to menu.");
            introVideo.loopPointReached -= OnVideoEnd;
            yield break;
        }

        introVideo.Play();

        // Determine the clip duration (seconds). Verified on Unity 6 WebGL that the
        // browser <video> element plays and ends correctly, but NONE of the Unity-side
        // signals (loopPointReached, time, isPlaying) are bridged back — so the finish
        // is driven by wall-clock time instead. The browser always plays the video at
        // real speed, so realtimeSinceStartup tracks it exactly; Time.deltaTime must
        // NOT be used here (it is clamped by maximumDeltaTime at low frame rates and
        // falls behind wall time, stretching the wait).
        double duration = introVideo.length;
        if (duration <= 0.0 && introVideo.frameRate > 0f)
        {
            duration = introVideo.frameCount / introVideo.frameRate;
        }
        if (duration <= 0.0)
        {
            duration = 13.7; // known length of intro_text.mp4 (13.625s), last resort
        }

        float playStart = Time.realtimeSinceStartup;
        float deadline = playStart + (float)duration + 0.25f;
        Debug.Log($"[Intro] Play. length={introVideo.length:F3} frameRate={introVideo.frameRate} " +
                  $"frameCount={introVideo.frameCount} -> duration={duration:F3}");

        float nextLog = playStart;
        while (!videoFinished && Time.realtimeSinceStartup < deadline)
        {
            // Fast-path exits in case the Unity-side bridge works (harmless if it never does).
            if (introVideo.time >= duration - 0.15)
            {
                break;
            }
            if (Time.realtimeSinceStartup >= nextLog)
            {
                nextLog = Time.realtimeSinceStartup + 2f;
                Debug.Log($"[Intro] wall={Time.realtimeSinceStartup - playStart:F2} " +
                          $"vp.time={introVideo.time:F2} isPlaying={introVideo.isPlaying}");
            }
            yield return null;
        }

        Debug.Log($"[Intro] exit. videoFinished={videoFinished} " +
                  $"wall={Time.realtimeSinceStartup - playStart:F2} vp.time={introVideo.time:F2}");
        introVideo.loopPointReached -= OnVideoEnd;
    }
}