using System.Collections;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoEndSceneLoader : MonoBehaviour
{
    public BuildConfig _buildConfig;

    [Header("Settings")]
    public VideoPlayer videoPlayer;
    public string sceneToLoad = "SceneLoader";


    // IMPORTANT: Use the PUBLIC URL (without the token) for caching to work
    public string videoUrl = "https://kumwxahsdnwyhbqabrwj.supabase.co/storage/v1/object/public/GameSplashVideo/intro.mp4";


    [Header("Controls")]
    public bool shouldPlayVideo;

    [SerializeField] private BackendBaseUrlController backendBaseUrlController;

    private bool videoDoneMethodCalled = false;

    public OnScreenKeyboardManager onScreenKeyboardManagerPrefab;
    public WebGLClipboard webGlClipboardPrefab;

    private void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Debug.unityLogger.logEnabled = false;
#endif
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

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("✅ Video Prepared from cache. Forcing Muted Autoplay...");

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

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;

        MainMenuAddressableHandler.LoadMainMenuData();

        //LoadingBridge.SceneToLoad = "Login";
        //LoadingBridge.ShowExtraImage = true;
        //LoadingBridge.IsAddressableScene = false;
        //LoadingBridge.pauseProfileApiCall = true;
        //SceneManager.LoadScene(sceneToLoad);
        SceneManager.LoadScene("Login");
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
}