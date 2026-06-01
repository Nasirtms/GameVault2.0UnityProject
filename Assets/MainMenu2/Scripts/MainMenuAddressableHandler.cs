using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class MainMenuAddressableHandler : MonoBehaviour
{
    static AsyncOperationHandle mainMenuDataHandle;
    static AsyncOperationHandle<SceneInstance> mainMenuSceneHandle;

    public static string mainMenuLabel = "mainmenu";
    public static string mainMenuSceneKey = "MainMenuScene";
    public static string sceneLoaderSceneKey = "SceneLoader";

    public static float DownloadProgress => mainMenuDataHandle.IsValid() ? mainMenuDataHandle.PercentComplete : 0f;
    public static float SceneLoadProgress => mainMenuSceneHandle.IsValid() ? mainMenuSceneHandle.PercentComplete : 0f;
    public static float TotalProgress
    {
        get
        {
            //float download = DownloadProgress;
            //float scene = SceneLoadProgress;

            if (mainMenuSceneHandle.IsValid())
                return SceneLoadProgress;       //return Mathf.Lerp(0.8f, 1f, SceneLoadProgress);

            return DownloadProgress * 0.8f;
        }
    }

    private static Task mainMenuDownloadTask;

    public static async Task LoadMainMenu()
    {
        if (mainMenuDownloadTask != null)
            await mainMenuDownloadTask;

        Scene previousScene = SceneManager.GetActiveScene();

        mainMenuSceneHandle = Addressables.LoadSceneAsync(mainMenuSceneKey, LoadSceneMode.Single);
        await mainMenuSceneHandle.Task;

        //LoadingBridge.SceneToLoad = mainMenuSceneKey;
        //LoadingBridge.ShowExtraImage = true;
        //LoadingBridge.IsAddressableScene = true;
        //SceneManagement.currentGameID = "";
        //Addressables.LoadSceneAsync(sceneLoaderSceneKey, LoadSceneMode.Single, true);    //SceneManager.LoadScene("SceneLoader");

        //await WaitForSeconds(2);
        //SceneManager.SetActiveScene(mainMenuSceneHandle.Result.Scene);
        //await WaitForSeconds(1);

        //if (previousScene.IsValid() && previousScene != mainMenuSceneHandle.Result.Scene)
        //    await SceneManager.UnloadSceneAsync(previousScene).AsTask();
    }

    public static Task LoadMainMenuData()
    {
        if (mainMenuDownloadTask != null)
            return mainMenuDownloadTask;

        mainMenuDownloadTask = LoadMainMenuDataInternal();
        return mainMenuDownloadTask;
    }

    private static async Task LoadMainMenuDataInternal()
    {
        Debug.Log($"[Addressables] Starting init for label={mainMenuLabel}");
        var init = Addressables.InitializeAsync();
        await init.Task;

        var sizeHandle = Addressables.GetDownloadSizeAsync(mainMenuLabel);
        await sizeHandle.Task;

        if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
            Debug.Log($"[Addressables] bytesToDownload={sizeHandle.Result}");

        mainMenuDataHandle = Addressables.DownloadDependenciesAsync(mainMenuLabel);
        await mainMenuDataHandle.Task;
    }

    private static async Task WaitForSeconds(float time)
    {
        float end = Time.time + time;
        while (Time.time < end)
            await Task.Yield();
    }
}
