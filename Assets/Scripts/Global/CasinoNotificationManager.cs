using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SerializableClasses;

public class CasinoNotificationManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Transform canvasTransform;

    [Header("Prefabs")]
    public GameObject notificationCanvasPrefab;
    public GameObject scrollEntryPrefab;

    [Header("UI References")]
    public GameObject notificationCanvas;
    public RectTransform notificationBar;
    public Button closeScrollContainerButton;
    public Transform verticalScrollContent;
    public Button moreButton;
    public GameObject scrollContainer;
    public RectTransform scrollContainerRect;
    private RectTransform moreButtonRect;
    private TextMeshProUGUI moreButtonTMP;

    [Header("More Button Positions")]
    public Vector2 arrowClosedPos;
    public Vector2 arrowOpenPos;

    [Header("Top Message")]
    public TextMeshProUGUI topMessageText;
    public float slideDuration = 0.4f;
    public float stayDuration = 2f;

    [Header("Other Settings")]
    public Vector2 barHidePosition;
    public Vector2 barShowPosition;
    public float barShowTimeDuration = 5;
    public float barHideTimeDuration = 3;
    private float barShowHideTimer = 0;
    private bool isBarVisible = false;
    private bool barShowHideInProgress = false;

#if UNITY_WEBGL
    private const int POOL_SIZE = 30;
#else
    private const int POOL_SIZE = 100;
#endif

    private bool isExpanded;
    private bool isSliding;
    private int currentTopIndex;

    private readonly List<string> notificationMessages = new();
    private readonly List<GameObject> entryPool = new();
    private int nextPoolIndex;

    // ------------------------------------------------------------
    // UNITY
    // ------------------------------------------------------------

  
    private void OnEnable()
    {
        //Debug.Log("[NOTIFY] Subscribed to FetchedNotifictionData");
        UserManager.FetchedNotifictionData += OnNotificationDataReceived;
    }

    private void OnDisable()
    {
        UserManager.FetchedNotifictionData -= OnNotificationDataReceived;
    }

    private void Update()
    {
        if (!barShowHideInProgress && !isExpanded)
        {
            barShowHideTimer += Time.deltaTime;
            if (barShowHideTimer >= (isBarVisible ? barShowTimeDuration : barHideTimeDuration))
            {
                ForceShowHideBar(!isBarVisible);
            }
        }
    }

    // ------------------------------------------------------------
    // EVENT ENTRY POINT (ONLY ENTRY)
    // ------------------------------------------------------------

    private void OnNotificationDataReceived(UserProfileResponse response)
    {
        //Debug.Log("[NOTIFY] Notification data received");

        if (response == null || response.recent_big_wins == null || response.recent_big_wins.Count == 0)
        {
            //Debug.Log("[NOTIFY] No notifications found");
            return;
        }

        EnsureCanvas();

        notificationMessages.Clear();

        foreach (var win in response.recent_big_wins)
        {
            string formatted = FormatNotificationMessage(win.message);
            notificationMessages.Add(formatted);
        }

        StartSlidingTopMessages();

        // If panel already open, refresh safely
        if (isExpanded)
        {
            StopAllCoroutines();
            StartCoroutine(PopulateScrollGradually());
        }
    }

    // ------------------------------------------------------------
    // UI INIT
    // ------------------------------------------------------------

    private void EnsureCanvas()
    {
        if (notificationCanvas != null)
            return;

        //Debug.Log("[NOTIFY] Creating notification canvas");

        notificationCanvas = Instantiate(notificationCanvasPrefab, canvasTransform);

        closeScrollContainerButton =
            notificationCanvas.transform.Find("CloseScrollButton").GetComponent<Button>();

        notificationBar =
            notificationCanvas.transform.Find("Notification").GetComponent<RectTransform>();

        moreButton =
            notificationCanvas.transform.Find("Notification/MoreButton").GetComponent<Button>();

        moreButtonRect = moreButton.GetComponent<RectTransform>();
        moreButtonTMP = moreButton.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        scrollContainer =
            notificationCanvas.transform.Find("Notification/ScrollContainer").gameObject;

        scrollContainerRect = scrollContainer.GetComponent<RectTransform>();

        verticalScrollContent =
            notificationCanvas.transform.Find("Notification/ScrollContainer/ScrollView/Viewport/Content");

        topMessageText =
            notificationCanvas.transform.Find("Notification/Notification/NotificationParent/NotificationText")
            .GetComponent<TextMeshProUGUI>();

        moreButton.onClick.AddListener(ToggleMore);
        closeScrollContainerButton.onClick.AddListener(ToggleMore);

        scrollContainerRect.localScale = new Vector3(1, 0, 1);
        closeScrollContainerButton.gameObject.SetActive(false);

        CreatePool();
    }

    // ------------------------------------------------------------
    // TOP SLIDER
    // ------------------------------------------------------------

    private void StartSlidingTopMessages()
    {
        if (notificationMessages.Count == 0 || isSliding)
            return;

        //Debug.Log("[NOTIFY] Start sliding messages");

        isSliding = true;
        currentTopIndex = 0;
        SlideNext();
    }

    private void SlideNext()
    {
        if (!isSliding || notificationMessages.Count == 0)
            return;

        RectTransform r = topMessageText.rectTransform;
        topMessageText.text = notificationMessages[currentTopIndex];

        r.DOKill();
        r.anchoredPosition = new Vector2(800, 0);

        r.DOAnchorPosX(0, slideDuration).OnComplete(() =>
        {
            DOVirtual.DelayedCall(stayDuration, () =>
            {
                r.DOAnchorPosX(-800, slideDuration).OnComplete(() =>
                {
                    currentTopIndex = (currentTopIndex + 1) % notificationMessages.Count;
                    SlideNext();
                });
            });
        });
    }

    // ------------------------------------------------------------
    // TOGGLE MORE (WEBGL SAFE)
    // ------------------------------------------------------------

    public void ToggleMore()
    {
        if (barShowHideInProgress)
            return;

        //Debug.Log("[NOTIFY] ToggleMore");

        isExpanded = !isExpanded;

        DOTween.Kill(scrollContainerRect);
        DOTween.Kill(moreButtonTMP);

        scrollContainerRect
            .DOScaleY(isExpanded ? 1 : 0, 0.3f)
            .OnComplete(Canvas.ForceUpdateCanvases);

        moreButtonRect
    .DOAnchorPos(isExpanded ? arrowOpenPos : arrowClosedPos, 0.3f);

        if (moreButtonTMP != null)
        {
            moreButtonTMP.DOKill();

            moreButtonTMP.DOFade(0f, 0.15f).OnComplete(() =>
            {
                moreButtonTMP.text = isExpanded ? "Show Less" : "Show More";
                moreButtonTMP.DOFade(1f, 0.15f);
            });
        }
        else
        {
            Debug.LogError("[NOTIFY] moreButtonTMP is NULL – check prefab hierarchy!");
        }


        closeScrollContainerButton.gameObject.SetActive(isExpanded);

        if (isExpanded)
        {
            StopAllCoroutines();
            StartCoroutine(PopulateScrollGradually());
        }
    }

    // ------------------------------------------------------------
    // POOL
    // ------------------------------------------------------------

    private void CreatePool()
    {
        //Debug.Log($"[POOL] Creating pool ({POOL_SIZE})");

        entryPool.Clear();
        nextPoolIndex = 0;

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject obj = Instantiate(scrollEntryPrefab, verticalScrollContent);
            obj.SetActive(false);
            entryPool.Add(obj);
        }
    }

    private IEnumerator PopulateScrollGradually()
    {
        //Debug.Log("[POOL] Populate start");

        foreach (var obj in entryPool)
            obj.SetActive(false);

        nextPoolIndex = 0;

        for (int i = 0; i < notificationMessages.Count && i < POOL_SIZE; i++)
        {
            AddEntry(notificationMessages[i]);

            if (i % 2 == 0)
                yield return null;
        }

        Canvas.ForceUpdateCanvases();
        //Debug.Log("[POOL] Populate done");
    }

    private void AddEntry(string msg)
    {
        //Debug.Log("[POOL] AddEntry");

        GameObject obj = entryPool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % POOL_SIZE;

        obj.transform.SetAsFirstSibling();

        var entry = obj.GetComponent<NotificationEntryUI>();
        entry.SetMessage(msg);

        //Debug.Log("[POOL] SetActive(true)");
        obj.SetActive(true);
        //Debug.Log("[POOL] Activated");
    }

    // ------------------------------------------------------------
    // FORMAT
    // ------------------------------------------------------------

    private string FormatNotificationMessage(string msg)
    {
        return msg.Replace("Congratulations", "<color=#dc8001>Congratulations</color>");
    }

    void ForceShowHideBar(bool state)
    {
        if (barShowHideInProgress)
            return;

        if (isBarVisible == state)
            return;

        barShowHideInProgress = true;
        barShowHideTimer = 0;

        notificationBar.DOAnchorPos(state ? barShowPosition : barHidePosition, 0.6f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            barShowHideInProgress = false;
            isBarVisible = state;
        });
    }
}
