using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LoadingBridge
{
    public static string SceneToLoad;             // The name / address of the scene to load
    public static bool ShowExtraImage;
    public static bool IsAddressableScene = true; // <-- new: mark true if the target scene is in Addressables
    public static bool pauseProfileApiCall = false; // <-- new: mark true if the target scene is in Addressables
}

public class FakeLoaderManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject fakeSceneLoaderBg;
    public RectTransform loadingBarFillRect;
    public GameObject gameTitle;
    public Sprite landscapeSprite;
    public Sprite portraitSprite;
    public GameObject loaderFrame;

    [Header("Settings")]
    public float fakeMinStepDelay = 0.15f;  // only used if we simulate
    public float fakeMaxStepDelay = 0.30f;  // only used if we simulate
    public float fakeStallChance = 0.2f;

    private float currentProgress = 0f;
    private bool isLoading = false;

    private void Start()
    {
        isLoading = false;

        if (!string.IsNullOrEmpty(LoadingBridge.SceneToLoad))
        {
            StartRealLoad(LoadingBridge.SceneToLoad, LoadingBridge.IsAddressableScene, LoadingBridge.ShowExtraImage);
        }

#if UNITY_ANDROID
        if(LoadingBridge.SceneToLoad == "GameRockPaperScissors")
        {
            Screen.orientation = ScreenOrientation.Portrait;
            fakeSceneLoaderBg.GetComponent<Image>().sprite = portraitSprite;
            fakeSceneLoaderBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            fakeSceneLoaderBg.GetComponent<RectTransform>().sizeDelta = new Vector2(1928.4f, 4160);

            loadingBarFillRect.GetComponent<RectTransform>().anchoredPosition = new Vector2(12.5f, 5.72f);
            loadingBarFillRect.GetComponent<RectTransform>().sizeDelta = new Vector2(12.5f, 0);

            loaderFrame.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -500);
            loaderFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(1201.743f, 56);

            gameTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 860);
            gameTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 533);
        }
#endif

        if (LoadingBridge.pauseProfileApiCall)
        {
            if (UserManager.Instance != null)
            {
                UserManager.Instance.StartUpdateCanAddCoin(false);
            }
            LoadingBridge.pauseProfileApiCall = false;
        }
    }

    /// <summary>
    /// Entry point for all scene loads
    /// </summary>
    public void StartRealLoad(string sceneName, bool isAddressable, bool titleImage = false)
    {
        if (isLoading) return;

        currentProgress = 0f;
        isLoading = true;

        if (gameTitle != null)
            gameTitle.SetActive(titleImage);

        StartCoroutine(RealLoadProgress(sceneName, isAddressable));
    }

    private IEnumerator RealLoadProgress(string sceneToLoad, bool isAddressable)
    {
        // Reset progress bar
        SetProgressUI(0f);

        if (isAddressable)
        {
            // ---------- ADDRESSABLE SCENE LOAD ----------
            var handle = Addressables.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single, true);

            while (!handle.IsDone)
            {
                float p = Mathf.Clamp01(handle.PercentComplete);
                SetProgressUI(p);
                yield return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[FakeLoaderManager] Addressables failed to load scene: " + handle.OperationException);
                yield break;
            }

            SetProgressUI(1f); // ensure full bar
        }
        else
        {
            // ---------- BUILT-IN SCENE LOAD ----------
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
            op.allowSceneActivation = false; // so we can fill up to 0.9f before activation

            while (op.progress < 0.9f)
            {
                SetProgressUI(op.progress);  // progress is 0..0.9f
                yield return null;
            }

            // final step: slide from 0.9 to 1.0 as we activate
            SetProgressUI(1f);
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                yield return null;
            }
        }

        isLoading = false;
    }



    private void SetProgressUI(float normalized)
    {
        // Adjust offsetMax.x to show fill
        Vector2 offsetMax = loadingBarFillRect.offsetMax;
        offsetMax.x = Mathf.Lerp(-1350f, 1f, normalized);
        loadingBarFillRect.offsetMax = offsetMax;
    }
}
