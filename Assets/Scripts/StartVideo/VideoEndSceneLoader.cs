using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoEndSceneLoader : MonoBehaviour
{
    public BuildConfig _buildConfig;
    [Header("Settings")]
    public VideoPlayer videoPlayer;
    public string sceneToLoad = "SceneLoader";
    public Button skipButton;

    // IMPORTANT: Use the PUBLIC URL (without the token) for caching to work
    public string videoUrl = "https://kumwxahsdnwyhbqabrwj.supabase.co/storage/v1/object/public/GameSplashVideo/intro.mp4";


    [Header("Controls")]
    public bool shouldPlayVideo;

    [SerializeField] private BackendBaseUrlController backendBaseUrlController;

    private bool videoDoneMethodCalled = false;

    [Header("Video Fallback")]
    [SerializeField] private float videoPrepareTimeout = 5f;
    private Coroutine fallbackRoutine;

    public OnScreenKeyboardManager onScreenKeyboardManagerPrefab;
    public WebGLClipboard webGlClipboardPrefab;

    private void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Debug.unityLogger.logEnabled = false;
#endif

        skipButton.onClick.AddListener(SkipVideo);
    }

    private void Start()
    {
        SceneManagement.buildConfig = _buildConfig;
        shouldPlayVideo = SceneManagement.buildConfig.isPlaySplashVideo;

        Debug.Log("URL: " + _buildConfig.GetUrl());
        Debug.Log("Game Type: " + _buildConfig.gameType);
        Debug.Log("Play Splash: " + _buildConfig.isPlaySplashVideo);


        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;

        if (backendBaseUrlController != null)
            SetBaseUrl(backendBaseUrlController.GetBaseUrl());

        if (shouldPlayVideo)
        {
            // Subscribe to events
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;

            fallbackRoutine = StartCoroutine(VideoFallbackTimer());
            // Start caching/preparing
            videoPlayer.Prepare();
        }
        else
        {
            OnVideoFinished(null);
        }

        Instantiate(onScreenKeyboardManagerPrefab);
        Instantiate(webGlClipboardPrefab);

        //        // Only executes when running as a WebGL build
        //#if UNITY_WEBGL && !UNITY_EDITOR
        //        WebGLInput.mobileKeyboardSupport = false;
        //#endif
    }
    private IEnumerator VideoFallbackTimer()
    {
        yield return new WaitForSeconds(videoPrepareTimeout);

        if (!videoDoneMethodCalled)
        {
            Debug.LogWarning("Video failed to prepare in time. Falling back to Login scene.");
            OnVideoFinished(null);
        }
    }
    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning("Video failed to load: " + message);
        OnVideoFinished(vp);
    }
    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("✅ Video Prepared from cache. Forcing Muted Autoplay...");

        if (fallbackRoutine != null)
        {
            StopCoroutine(fallbackRoutine);
            fallbackRoutine = null;
        }

        //skipButton.gameObject.SetActive(true);

        // 1. Mute the video player immediately. 
        // This bypasses the 'AudioContext' block you are seeing.
        vp.SetDirectAudioMute(0, true);
        vp.Play();

#if UNITY_WEBGL && !UNITY_EDITOR
        // 2. Tell the browser: 'Play this immediately because it is muted'
        // Then, the moment the user clicks ANYWHERE, unmute it.
        Application.ExternalEval(@"
            var video = document.querySelector('video');
            if (video) {
                video.muted = true;
                video.play();

                // Create a one-time listener to unmute once the user interacts
                var unmute = function() {
                    video.muted = false;
                    console.log('🔊 Audio Context Unlocked by click');
                };
                window.addEventListener('click', unmute, {once: true});
                window.addEventListener('touchstart', unmute, {once: true});
            }
        ");
#endif
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        //if (videoDoneMethodCalled)
        //    return;

        //videoDoneMethodCalled = true;

        if (fallbackRoutine != null)
        {
            StopCoroutine(fallbackRoutine);
            fallbackRoutine = null;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;

        MainMenuAddressableHandler.LoadMainMenuData();

        //LoadingBridge.SceneToLoad = "Login";
        //LoadingBridge.ShowExtraImage = true;
        //LoadingBridge.IsAddressableScene = false;
        //LoadingBridge.pauseProfileApiCall = true;
        //SceneManager.LoadScene(sceneToLoad);
        SceneManager.LoadSceneAsync("Login");
    }

    void SetBaseUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Debug.Log($"Base Url : {url}");

//#if UNITY_EDITOR
//        url = "http://localhost:5036";
//#endif

        ApiEndpoints.UpdataBaseUrl(url);

    }

    void SkipVideo()
    {
        Debug.Log("Skip Video Clicked");
        OnVideoFinished(null);
    }
}