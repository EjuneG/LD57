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

        // Determine the clip duration (seconds). On Unity 6 WebGL, loopPointReached is
        // not bridged from the browser, but time/length are (verified) — so the finish
        // is driven primarily by playback position. A network buffering stall pauses
        // the browser video, so position (not wall time) is the only honest signal;
        // a pure wall-clock deadline would cut the video short whenever it buffers.
        // Time.deltaTime must not be used for any of this (it is clamped by
        // maximumDeltaTime at low frame rates and falls behind wall time).
        double duration = introVideo.length;
        if (duration <= 0.0 && introVideo.frameRate > 0f)
        {
            duration = introVideo.frameCount / introVideo.frameRate;
        }
        // intro_text.mp4 is 13.625s; a wildly different readout means the metadata
        // bridge handed back garbage, so fall back to the known length.
        if (duration < 5.0 || duration > 60.0)
        {
            duration = 13.7;
        }

        float playStart = Time.realtimeSinceStartup;
        double lastPosition = 0.0;
        float lastProgressWall = playStart;
        float nextLog = playStart;
        const float stallBailSeconds = 6f; // give up if playback makes no progress this long

        Debug.Log($"[Intro] Play. length={introVideo.length:F3} frameRate={introVideo.frameRate} " +
                  $"frameCount={introVideo.frameCount} -> duration={duration:F3}");

        while (!videoFinished)
        {
            float wall = Time.realtimeSinceStartup;
            double position = introVideo.time;

            if (position >= duration - 0.15)
            {
                break; // reached the actual end of the clip
            }

            if (position > lastPosition + 0.01)
            {
                lastPosition = position;
                lastProgressWall = wall;
            }

            if (lastPosition > 0.5)
            {
                // Position reporting works: wait on real progress, so buffering just
                // extends the wait. Only bail if playback is wedged for good.
                if (wall - lastProgressWall > stallBailSeconds)
                {
                    Debug.LogWarning($"[Intro] video stalled at {lastPosition:F2}s " +
                                     $"for {stallBailSeconds}s; skipping to menu.");
                    break;
                }
            }
            else if (wall > playStart + (float)duration + 0.25f)
            {
                // Position never got bridged on this browser: wall-clock fallback.
                break;
            }

            if (wall >= nextLog)
            {
                nextLog = wall + 2f;
                Debug.Log($"[Intro] wall={wall - playStart:F2} vp.time={position:F2} " +
                          $"isPlaying={introVideo.isPlaying}");
            }
            yield return null;
        }

        Debug.Log($"[Intro] exit. videoFinished={videoFinished} " +
                  $"wall={Time.realtimeSinceStartup - playStart:F2} vp.time={introVideo.time:F2}");

        // Whatever ended the wait, don't leave the video playing behind the menu.
        introVideo.Stop();
        introVideo.loopPointReached -= OnVideoEnd;
    }
}