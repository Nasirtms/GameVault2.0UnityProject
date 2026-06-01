using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class SceneManagement
{
    //Game Type
    public static SceneAccessType sceneAccessType;
    public static BuildConfig buildConfig;

    //Current Game Scene Name
    public static string currentGameName;
    public static string currentGameID;

    //MainMenuData
    public static bool isShowSpinWheel = false;
    public static bool isShowEventPopup = false;
    public static bool isShowNotificationAfterLogin = false;
    public static bool dailySpiinLimit = false;
    public static int DailyRewardValue;
    public static string lastOpenedGameCategory;

    //Events Details
    public static bool EventshowOnce;
    public static bool eventIsActive;
    public static bool eventIsEnded;
    public static string EventtargetUser;
    public static string eventHeading;
    public static string evntBottom;
    public static string evntMassage;
    public static List<SerializableClasses.Events> events = new();

    //Login Notification Details
    public static List<SerializableClasses.LoginNotification> LatestNotifications = new();
    public static string loginNotificationtargetUser;
    public static string notificationTitle;
    public static string notificationMessage;
    public static string notificationImageUrl;

    ////Random Data
    //public static float coinBalanceBeforeGoingInGame;


    //Game List
    public static List<SerializableClasses.Game> games = new List<SerializableClasses.Game>();

    //Game Data
    //public static string GameId = "6934d888-9f98-4b99-8250-6e21331606b4";

    //Profile Images 
    public static List<string> profile_iconUrls = new List<string>();

    //Top 100 user Leaderboard
    public static List<SerializableClasses.LeaderboardEntry> weeklyLeaderboard = new List<SerializableClasses.LeaderboardEntry>();



    public static void UpdateCurrentSceneName(string sceneName)
    {
        currentGameName = sceneName;
        if (SlotSpinService.Instance != null)
        {
            SlotSpinService.Instance.GameScenName = sceneName;
        }
    }

    public static void ResetSceneManagementData()
    {
        // Strings
        currentGameName = "";
        EventtargetUser = "";
        eventHeading = "";
        evntBottom = "";
        evntMassage = "";
        notificationTitle = "";
        notificationMessage = "";
        notificationImageUrl = "";
        currentGameID = "";
        DailyRewardValue = 0;

        //GameId = "";
        loginNotificationtargetUser = "";

        // Booleans
        isShowSpinWheel = false;
        isShowEventPopup = false;
        isShowNotificationAfterLogin = false;
        dailySpiinLimit = false;
        eventIsActive = false;
        eventIsEnded = false;

        // Lists
        LatestNotifications.Clear();
        events.Clear();
        games.Clear();
        profile_iconUrls.Clear();
        weeklyLeaderboard.Clear();

        Debug.Log("✅ SceneManagement data has been reset.");
    }

    //public static async void GoBackToMainMenu(bool forceShowLoading = true)
    //{
    //    bool isCached = await IsAddressableCached(MainMenuAddressableHandler.sceneLoaderSceneKey);

    //    if (forceShowLoading && !isCached)
    //    {
    //        Debug.Log("SceneManagement-GoBackToMainMenu- ForceShowLoadingPanel");
    //        ForceShowLoadingPanel();
    //    }
    //    else
    //    {
    //        Debug.Log($"SceneManagement-GoBackToMainMenu- forceShowLoading: {forceShowLoading} -- isCached: {isCached}");
    //    }

    //    LoadingBridge.SceneToLoad = MainMenuAddressableHandler.mainMenuSceneKey;
    //    LoadingBridge.ShowExtraImage = true;
    //    LoadingBridge.IsAddressableScene = true;
    //    SceneManagement.currentGameID = "";
    //    Addressables.LoadSceneAsync(MainMenuAddressableHandler.sceneLoaderSceneKey, LoadSceneMode.Single, true);    //SceneManager.LoadScene("SceneLoader");
    //}

    public static void GoBackToMainMenu(bool forceShowLoading = true)
    {
        if (forceShowLoading)
            ForceShowLoadingPanel();

        ApiHandler.instance?.GameExited(SceneManagement.currentGameID);

        LoadingBridge.SceneToLoad = MainMenuAddressableHandler.mainMenuSceneKey;
        LoadingBridge.ShowExtraImage = true;
        LoadingBridge.IsAddressableScene = true;
        SceneManagement.currentGameID = "";
        Addressables.LoadSceneAsync(MainMenuAddressableHandler.sceneLoaderSceneKey, LoadSceneMode.Single, true);    //SceneManager.LoadScene("SceneLoader");
    }

    static void ForceShowLoadingPanel()
    {
        MainMenu.UILoadingPanel loadingPanel;
        MainMenu.UILoadingPanel loadingPanelPrefab = Resources.Load<MainMenu.UILoadingPanel>("LoadingPanelWithCanvas");

        loadingPanel = GameObject.Instantiate(loadingPanelPrefab);
        loadingPanel.OpenPanel(.2f, .3f);
    }

    public static async Task<bool> IsAddressableCached(object key)
    {
        var handle = Addressables.GetDownloadSizeAsync(key);
        await handle.Task;

        bool isCached = false;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            isCached = handle.Result == 0;
        }

        Addressables.Release(handle);
        return isCached;
    }
}