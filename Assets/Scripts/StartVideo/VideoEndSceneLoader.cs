using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using static System.Net.WebRequestMethods;

public class VideoEndSceneLoader : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string sceneToLoad = "SceneLoader";
    public static string SheetURL = "";

    [SerializeField] private BackendBaseUrlController backendBaseUrlController;

    private void Start()
    {
        if (backendBaseUrlController == null)
        {
            Debug.LogError("❌ BackendBaseUrlController is not assigned in Inspector!");
            return;
        }

#if UNITY_WEBGL
        Debug.Log("🌐 WebGL build detected → Skipping video and loading scene directly.");
        SetBaseUrl(backendBaseUrlController.GetBaseUrl());
        StartCoroutine(LoadSceneFast());
        return;
#else
        GlobleSoundManager.Instance.PlayMusic("MainMenuMusic");
        videoPlayer.loopPointReached += OnVideoFinished;
#endif

        if (backendBaseUrlController.environment == BackendEnvironment.Local_ngrok)
        {
            StartCoroutine(FetchURLFromGoogleSheet());
        }
        else if (backendBaseUrlController.environment == BackendEnvironment.Production_AWS_Server)
        {
            SetBaseUrl(backendBaseUrlController.GetBaseUrl());
#if UNITY_EDITOR
            StartCoroutine(LoadSceneFast());
#endif
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadingBridge.SceneToLoad = "Login";
        LoadingBridge.ShowExtraImage = true;
        LoadingBridge.IsAddressableScene = false;
        SceneManager.LoadScene(sceneToLoad);
    }

    IEnumerator StopVideoPlayer()
    {
        videoPlayer.Stop();
        yield return new WaitForEndOfFrame();
        OnVideoFinished(videoPlayer);
    }

    IEnumerator FetchURLFromGoogleSheet()
    {
        string sheetCSVUrl = "https://docs.google.com/spreadsheets/d/1qGIrB234DwCmyzF6DnOnw6415LRl1T2x-Sx8rC6NTf0/export?format=csv";

        using (UnityWebRequest www = UnityWebRequest.Get(sheetCSVUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string csvText = www.downloadHandler.text;
                string[] rows = csvText.Split('\n');

                if (rows.Length > 0)
                {
                    string[] columns = rows[0].Split(',');
                    SheetURL = columns[0].Trim();

                    Debug.Log("✅ URL from Google Sheet (A1): " + SheetURL);

                    SetBaseUrl(SheetURL);

#if UNITY_EDITOR
                    StartCoroutine(LoadSceneFast());
#endif
                }
            }
            else
            {
                Debug.LogError("❌ Failed to download sheet: " + www.error);
            }
        }
    }

    void SetBaseUrl(string url)
    {
        Debug.Log($"Base Url : {url}");

#if UNITY_EDITOR
        url = "http://localhost:5036";
#endif
        ApiEndpoints.UpdataBaseUrl(url);
    }

    IEnumerator LoadSceneFast()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        yield return new WaitForEndOfFrame();
        OnVideoFinished(videoPlayer);
    }
}