using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SerializableClasses;

public class CasinoNotificationManager : MonoBehaviour
{
    public static CasinoNotificationManager Instance;

    [Header("Canvas")]
    [SerializeField] private Transform canvasTransform;

    [Header("Prefabs")]
    public GameObject notificationCanvasPrefab;
    public GameObject scrollEntryPrefab;

    [Header("UI References")]
    public GameObject notificationCanvas;
    public Button closeScrollContainerButton;
    public Transform verticalScrollContent;
    public Button moreButton;
    public Image moreArrowIcon;
    public GameObject scrollContainer;
    public RectTransform scrollContainerRect;
    private TextMeshProUGUI moreButtonTMP;
    

    [Header("More Button Position")]
    public Vector2 arrowClosedPos;   // Y position when panel is closed
    public Vector2 arrowOpenPos;     // Y position when panel is expanded


    [Header("Top Sliding Message")]
    public TextMeshProUGUI topMessageText;
    public float slideDuration;
    public float stayDuration;

    [Header("Settings")]
    public float displayDelay;
    public int maxScrollEntries;

    // STATE
    private bool canDisplayNotifications = false;
    private bool isExpanded = false;
    private bool isSliding = false;
    private int currentTopIndex = 0;

    public List<string> notificationMessages = new List<string>();
    [SerializeField] private UserProfileResponse _userProfileResponse;

    // POOL
    private const int POOL_SIZE = 100;
    private List<GameObject> entryPool = new List<GameObject>();
    private int nextPoolIndex = 0;


    // ------------------------------------------------------------
    // UNITY
    // ------------------------------------------------------------
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UserManager.FetchedNotifictionData += UpdateBigWinNotificationDataMethod;
    }

    private void OnDestroy()
    {
        UserManager.FetchedNotifictionData -= UpdateBigWinNotificationDataMethod;
    }


    // ------------------------------------------------------------
    // FETCH NOTIFICATION DATA
    // ------------------------------------------------------------
    public void UpdateBigWinNotificationDataMethod(UserProfileResponse userProfileResponse)
    {
        if (userProfileResponse.recent_big_wins.Count == 0)
            return;

        _userProfileResponse = userProfileResponse;

        foreach (var win in userProfileResponse.recent_big_wins)
        {
            string coloredMessage = FormatNotificationMessage(win.message);
            notificationMessages.Add(coloredMessage);

            // ⭐ If panel is expanded, push new entry instantly to top
            if (isExpanded)
                AddPooledEntry(win.message);
        }


        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    private string FormatNotificationMessage(string msg)
    {
        // 1. Extract player name (first word after "to ")
        string playerName = "";
        int toIndex = msg.IndexOf("to ");
        int forIndex = msg.IndexOf(" for ");
        if (toIndex >= 0 && forIndex > toIndex)
            playerName = msg.Substring(toIndex + 3, forIndex - (toIndex + 3));

        // 2. Extract win amount (number before " in ")
        string winAmount = "";
        int winningIdx = msg.IndexOf("winning ");
        int inIdx = msg.IndexOf(" in ");
        if (winningIdx >= 0 && inIdx > winningIdx)
            winAmount = msg.Substring(winningIdx + 8, inIdx - (winningIdx + 8));

        // 3. Build formatted message
        return msg
            .Replace("Congratulations", "<color=#dc8001>Congratulations</color>")
            .Replace(playerName, $"<color=#00e4ff>{playerName}</color>")
            .Replace(winAmount, $"<b><size=115%><color=#FFD700>{winAmount}</color></size></b>");

    }


    // ------------------------------------------------------------
    // SCENE LOADED
    // ------------------------------------------------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main" || scene.name.StartsWith("Game"))
        {
            canDisplayNotifications = true;
            StartCoroutine(StartNotificationDisplayAfterDelay());
        }
        else
        {
            canDisplayNotifications = false;
            if (notificationCanvas != null)
                notificationCanvas.SetActive(false);

            isSliding = false;
        }
    }


    // ------------------------------------------------------------
    // INIT UI CANVAS
    // ------------------------------------------------------------
    private IEnumerator StartNotificationDisplayAfterDelay()
    {
        yield return new WaitForSeconds(displayDelay);

        if (!canDisplayNotifications) yield break;

        if (notificationCanvas == null)
        {
            notificationCanvas = Instantiate(notificationCanvasPrefab, canvasTransform);


            closeScrollContainerButton = notificationCanvas.transform.Find("CloseScrollButton").GetComponent<Button>();

            moreButton = notificationCanvas.transform.Find("Notification/MoreButton").GetComponent<Button>();
            moreArrowIcon = moreButton.GetComponent<Image>();
            moreButtonTMP = moreButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            
            scrollContainer = notificationCanvas.transform.Find("Notification/ScrollContainer").gameObject;
            scrollContainerRect = scrollContainer.GetComponent<RectTransform>();
            
            verticalScrollContent = notificationCanvas.transform.Find("Notification/ScrollContainer/ScrollView/Viewport/Content");

            topMessageText = notificationCanvas.transform.Find("Notification/Notification/NotificationParent/NotificationText").GetComponent<TextMeshProUGUI>();

            // Button listeners
            closeScrollContainerButton.onClick.RemoveAllListeners();
            closeScrollContainerButton.onClick.AddListener(ToggleMore);

            

            moreButton.onClick.RemoveAllListeners();
            moreButton.onClick.AddListener(ToggleMore);
            // Initialize
            closeScrollContainerButton.gameObject.SetActive(false);
            scrollContainerRect.localScale = new Vector3(1, 0, 1);

            CreateScrollPool();
        }

        notificationCanvas.SetActive(true);

        // Start sliding animation
        StartSlidingTopMessages();
    }

    private void StopSlidingTopMessages()
    {
        isSliding = false;

        if (topMessageText != null)
        {
            // freeze current position
            var r = topMessageText.GetComponent<RectTransform>();
            r.DOKill();   // stop any running tweens
        }
    }

    // ------------------------------------------------------------
    // TOP SLIDE SYSTEM (DOTween)
    // ------------------------------------------------------------
    public void StartSlidingTopMessages()
    {
        if (notificationMessages.Count == 0)
            return;

        isSliding = true;
        currentTopIndex = 0;
        SlideNextMessage();
    }

    private void SlideNextMessage()
    {
        if (!isSliding || notificationMessages.Count == 0)
            return;

        RectTransform r = topMessageText.rectTransform;
        topMessageText.text = notificationMessages[currentTopIndex];

        r.DOKill();
        r.anchoredPosition = new Vector2(800f, 0f);

        // Slide IN
        r.DOAnchorPosX(0f, slideDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            DOVirtual.DelayedCall(stayDuration, () =>
            {
                if (!isSliding) return;

                // Slide OUT
                r.DOAnchorPosX(-800f, slideDuration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    currentTopIndex = (currentTopIndex + 1) % notificationMessages.Count;
                    SlideNextMessage();
                });

            });
        });
    }


    // ------------------------------------------------------------
    // TOGGLE MORE PANEL
    // ------------------------------------------------------------+
    public void ToggleMore()
    {
        try
        {

            Debug.Log("ToggleMore ");
            if (scrollContainerRect == null) return;
            MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
            isExpanded = !isExpanded;

            Debug.Log("ToggleMore 111");

            float targetScale = isExpanded ? 1f : 0f;
            string targetText = isExpanded ? "Show Less" : "Show More";
            Vector2 targetArrowPos = isExpanded ? arrowOpenPos : arrowClosedPos;
            float arrowRotation = isExpanded ? 180f : 0f;

            Debug.Log("ToggleMore 222");
            // Kill old tweens cleanly
            scrollContainerRect.DOKill();
            Debug.Log("ToggleMore 333");
            moreArrowIcon.rectTransform.DOKill();
            Debug.Log("ToggleMore 444");
            moreButtonTMP.DOKill();
            Debug.Log("ToggleMore 555");
            moreButton.transform.DOKill();  // ⭐ important
            Debug.Log("ToggleMore 666");
            closeScrollContainerButton.transform.DOKill();
            Debug.Log("ToggleMore 777");

            // Animate Scroll Container
            scrollContainerRect
                .DOScaleY(targetScale, 0.35f)
                .SetEase(Ease.InOutSine)
                .OnUpdate(RebuildLayout)
                .OnComplete(RebuildLayout);

            Debug.Log("ToggleMore 888");

            // Move arrow
            moreArrowIcon.rectTransform
                .DOAnchorPos(targetArrowPos, 0.35f)
                .SetEase(Ease.InOutSine);

            Debug.Log("ToggleMore 999");

            // Animate text change
            moreButtonTMP.DOFade(0f, 0.15f).OnComplete(() =>
            {
                moreButtonTMP.text = targetText;
                moreButtonTMP.DOFade(1f, 0.15f);
            });

            Debug.Log("ToggleMore 101010");

            // UI Logic
            closeScrollContainerButton.gameObject.SetActive(isExpanded);

            Debug.Log("ToggleMore 111111");
            CylindricalUIWarpSwipe.isDragable = !isExpanded;

            Debug.Log("ToggleMore 121212");

            if (isExpanded)
                FetchAndPopulateScrollEntries();
            //StartCoroutine(FetchAndPopulateScrollEntries());

            Debug.Log("ToggleMore 131313");
            //else
            //    StartSlidingTopMessages();

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Notifications Show More Button: ex.Message: {ex.Message} ___ ex.Source: {ex.Source} ___ ex.InnerException: {ex.InnerException} ___ ex.StackTrace: {ex.StackTrace}");
        }
    }

    private void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            notificationCanvas.GetComponent<RectTransform>()
        );
    }


    // ------------------------------------------------------------
    // SCROLL LIST SYSTEM (POOL)
    // ------------------------------------------------------------

    private void CreateScrollPool()
    {
        entryPool.Clear();
        nextPoolIndex = 0;

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject obj = Instantiate(scrollEntryPrefab, verticalScrollContent);
            obj.SetActive(false);

            entryPool.Add(obj);
        }
    }

    private void FetchAndPopulateScrollEntries()
    {

        Debug.Log("ToggleMore 222 111");

        //yield return new WaitForSeconds(0.5f);

        Debug.Log("ToggleMore 222 222");

        ClearScroll();

        Debug.Log("ToggleMore 222 333");

        PopulateScrollEntries(notificationMessages);

        Debug.Log("ToggleMore 222 444");

    }

    private void ClearScroll()
    {

        Debug.Log("ToggleMore 333 111 entryPool: " + entryPool.Count);

        foreach (var entry in entryPool)
            entry.SetActive(false);

        Debug.Log("ToggleMore 333 222 entryPool: " + entryPool.Count);

        nextPoolIndex = 0;
    }

    public void PopulateScrollEntries(List<string> entries)
    {
        Debug.Log("ToggleMore 444 111 entries: " + entries.Count);
        foreach (string msg in entries)
            AddPooledEntry(msg);
    }

    private void AddPooledEntry(string message)
    {
        Debug.Log("ToggleMore 555 111");
        GameObject obj = entryPool[nextPoolIndex];
        Debug.Log("ToggleMore 555 222");
        nextPoolIndex = (nextPoolIndex + 1) % POOL_SIZE;
        Debug.Log("ToggleMore 555 333");

        // ⭐ Make new entry go to TOP
        obj.transform.SetAsFirstSibling();
        Debug.Log("ToggleMore 555 444");

        var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
        Debug.Log("ToggleMore 555 555");
        tmp.text = message;
        Debug.Log("ToggleMore 555 666");


        obj.SetActive(true);
        Debug.Log("ToggleMore 555 777");
    }
}
