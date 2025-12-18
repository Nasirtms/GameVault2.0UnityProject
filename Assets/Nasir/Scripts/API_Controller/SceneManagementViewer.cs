using UnityEngine;
using System.Collections.Generic;

public class SceneManagementViewer : MonoBehaviour
{
    private static SceneManagementViewer instance;

    [Header("Current Active Game")]
    [SerializeField] private SceneAccessType sceneAccessType;
    [SerializeField] private string currentSceneName;
    [SerializeField] private string currentGameID;

    [Header("Avatar URL")]
    public List<string> profile_iconUrls = new List<string>();


    [Header("Main Menu Flags")]
    [SerializeField] private bool isShowSpinWheel;
    [SerializeField] private bool isShowEventPopup;
    [SerializeField] private bool isShowNotificationAfterLogin;
    [SerializeField] private int DailyRewardValue;
    [SerializeField] private bool dailySpinLimit;
    [SerializeField] private string lastOpenedGameCategory;

    [Header("Event Info")]
    [SerializeField] private bool EventshowOnce;
    [SerializeField] private bool eventIsActive;
    [SerializeField] private bool eventIsEnded;
    [SerializeField] private string targetUser;
    [SerializeField] private string eventHeading;
    [SerializeField] private string eventBottom;
    [SerializeField] private string eventMessage;
    [SerializeField] private List<SerializableClasses.Events> eventsList = new();

    [Header("Login Notification Info")]
    [SerializeField] private List<SerializableClasses.LoginNotification> LatestNotifications = new();
    [SerializeField] private string loginNotificationtargetUser;
    [SerializeField] private string notificationTitle;
    [SerializeField] private string notificationHeading;
    [SerializeField] private string notificationImageUrl;

    [Header("Games List")]
    [SerializeField] private List<SerializableClasses.Game> gamesList = new();
    [SerializeField] private List<string> registeredKeys = new();

    [SerializeField] private List<SerializableClasses.LeaderboardEntry> _weeklyLeaderboard = new();
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateView), 0.5f, 1f);
    }

    private void OnValidate()
    {
        registeredKeys.Clear();
        registeredKeys.AddRange(GameSlotRegistry.GetAllKeys());
    }


    private void UpdateView()
    {
        ClearData();

        sceneAccessType = SceneManagement.sceneAccessType;
        currentSceneName = SceneManagement.currentGameName;
        currentGameID = SceneManagement.currentGameID;

        registeredKeys.AddRange(GameSlotRegistry.GetAllKeys());

        // Main Menu Flags
        isShowSpinWheel = SceneManagement.isShowSpinWheel;
        isShowEventPopup = SceneManagement.isShowEventPopup;
        isShowNotificationAfterLogin = SceneManagement.isShowNotificationAfterLogin;
        dailySpinLimit = SceneManagement.dailySpiinLimit;
        DailyRewardValue = SceneManagement.DailyRewardValue;
        lastOpenedGameCategory = SceneManagement.lastOpenedGameCategory;

        // Events
        EventshowOnce = SceneManagement.EventshowOnce;
        eventIsActive = SceneManagement.eventIsActive;
        eventIsEnded = SceneManagement.eventIsEnded;
        targetUser = SceneManagement.EventtargetUser;
        eventHeading = SceneManagement.eventHeading;
        eventBottom = SceneManagement.evntBottom;
        eventMessage = SceneManagement.evntMassage;
        eventsList = new List<SerializableClasses.Events>(SceneManagement.events);

        LatestNotifications = new List<SerializableClasses.LoginNotification>(SceneManagement.LatestNotifications);

        // Login Notification
        loginNotificationtargetUser = SceneManagement.loginNotificationtargetUser;
        notificationTitle = SceneManagement.notificationTitle;
        notificationHeading = SceneManagement.notificationMessage;
        notificationImageUrl = SceneManagement.notificationImageUrl;

        profile_iconUrls = new List<string>(SceneManagement.profile_iconUrls);

        // Games
        gamesList = new List<SerializableClasses.Game>(SceneManagement.games);

        _weeklyLeaderboard = new List<SerializableClasses.LeaderboardEntry>(SceneManagement.weeklyLeaderboard);
    }


    void ClearData()
    {
        registeredKeys.Clear();

    }
}
