using System;
using System.Collections.Generic;
using UnityEngine;

public static class SceneManagement
{
    //Game Type
    public static SceneAccessType sceneAccessType;

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

}