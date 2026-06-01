using DG.Tweening;
using MainMenu;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static SerializableClasses;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance { get; private set; }

    public string UserID { get; private set; }
    public string Username { get; private set; }
    public int avatarIndex { get; private set; }
    public float Coins { get; private set; }
    public Sprite AvatarImage;// { get; private set; }
    public string Bio { get; private set; }

    public bool isShowNotification = false;
    public bool isShowEvent = false;

    [Header("Prefabs")]
    [SerializeField] private GameObject MidNightPartBtn_Prefab;
    [SerializeField] private Transform MidNightPartBtn_Parent;


    [Header("Toggle Menu Button")]
    [SerializeField] private Button menuToggleBtn;
    [SerializeField] private RectTransform menuRectTransform;
    [SerializeField] private bool menuIsOpen;
    [SerializeField] private float menuduration;

    [Header("User Info Display")]
    public TextMeshProUGUI userIdText;
    public TextMeshProUGUI userCoinsText;
    public Sprite userAvatarDefaultImage;
    public Image userAvatar;
    public bool useAvatarDummyImage = false;
    public Sprite userAvatarDummyImage;

    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Popup Buttons")]
    public Button profileButton;
    public Button midnightPartyButton;
    public Button leaderboardButton;
    public Button changePasswordButton;
    public Button settingButton;
    public Button spinWheelButton;
    public Button feedbackButton;

    [Header("Avatar Change Button & Canvas")]
    public Button AvaarChangeLeftArrow_Btn;
    public Button AvaarChangeRightArrow_Btn;
    public Button AvatarSelect_Btn;
    public Button AvatarSelectClose_Btn;
    public RawImage AvaarChangeAvatar_RI;
    public RawImage AvaarChangeAvatarTrans_RI;

    [Header("Spin Button")]
    [SerializeField] private GameObject _spinBtnGO;

    [Header("Popups")]
    public Transform PopupParent;
    public GameObject profilePopup;
    public GameObject leaderboardPopup;
    public GameObject changePasswordPopup;
    public GameObject settingsPopup;
    public FeedbackPanel feedbackPopup;


    [Header("Click effect")]
    public GameObject clickEffectPrefab;
    public float effectDuration = 0.3f;
    public ParticleSystem mouseTrailParticle;


    private Tween tween;
    public UnityEvent OnUserDataUpdated = new UnityEvent();

    public static event Action PopupShown;

    public event Action ToggleAvatarSelectButton;

    public List<string> gamesNames = new List<string>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        Instance = this;

        //Debug.Log("MainMenuUIManager: Instance Created");


    }

    bool setDefaultAvatarIcon;
    void Start()
    {
        setDefaultAvatarIcon = true;
        SetUserData();
        SetSceneData();
        StartCoroutine(WaitForFirstClickToStartInactivity());
    }




    public void ToggleMenuButtonsUI(bool b = true)
    {
        if (!b)
        {
            menuIsOpen = b;
        }
        else
        {
            menuIsOpen = !menuIsOpen;
        }

        if (menuIsOpen)
        {
            menuRectTransform.DOScaleY(1f, menuduration).SetEase(Ease.OutBack);
        }
        else
        {
            menuRectTransform.DOScaleY(0f, menuduration).SetEase(Ease.InBack);
        }
    }

    private IEnumerator WaitForFirstClickToStartInactivity()
    {
        //Debug.Log("👆 Waiting for first click/tap to start inactivity tracking...");

        // Wait until a click or touch is detected
        bool clicked = false;
        while (!clicked)
        {
            if (Input.GetMouseButtonDown(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                clicked = true;
                //Debug.Log("click detected — starting inactivity watcher.");
                if (ClickSessionManager.Instance != null)
                    ClickSessionManager.Instance.StartInactivityTracking();
            }

            yield return null;
        }
    }

    public void SetSceneData()
    {
        SpinWheelButtonAnimator spinWheelAnimator;
        if (_spinBtnGO != null)
        {
            spinWheelAnimator = _spinBtnGO.transform.GetComponent<SpinWheelButtonAnimator>();

            if (spinWheelAnimator != null)
            {
                spinWheelAnimator.SetActiveState(SceneManagement.isShowSpinWheel);
                HideAllPopups();
                SetupMenuButtons();

                isShowNotification = SceneManagement.isShowNotificationAfterLogin;
                isShowEvent = SceneManagement.isShowEventPopup;

                if (SceneManagement.isShowNotificationAfterLogin || SceneManagement.isShowEventPopup)
                {
                    SetActiveMidNightPartyBtn();
                }
            }
            else
            {
                Debug.LogWarning("nasir_warning spinWheelAnimator not found ");
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            SpawnEffectAtMousePosition("Click");
        }

        if (Input.GetMouseButton(0))
        {
            if (mouseTrailParticle != null)
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                mouseTrailParticle.transform.position = pos;
            }
        }

        //if (Input.mouseScrollDelta.y != 0) // Scroll wheel moved
        //{
        //    SpawnEffectAtMousePosition("Scroll");
        //}
    }

    #region Loder

    public void StartRotation()
    {
        //Debug.Log("Spinner StartRotation");
        if (spinnerImage == null) return;

        spinnerImage.gameObject.SetActive(true);
        // Kill any existing tween
        tween?.Kill();

        // Calculate how long one full rotation takes
        float duration = 360f / rotationSpeed;

        tween = spinnerImage
            .DORotate(new Vector3(0, 0, -360f), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopRotation()
    {
        //Debug.Log("Spinner StopRotation");
        tween?.Kill();
        if (spinnerImage != null)
        {
            spinnerImage.gameObject.SetActive(false);
        }
    }

    #endregion

    void SetActiveMidNightPartyBtn()
    {
        string Eventtype = string.Empty;
        string Notificationtype = string.Empty;
        bool isShow = false;
        bool EventisActive = SceneManagement.eventIsActive;
        bool NotificationActive = false;

        string userType = UserManager.Instance?.userType?.ToLower();
        string userId = UserManager.Instance?.UserId;

        if (string.IsNullOrEmpty(userType) || string.IsNullOrEmpty(userId))
            return;

        // Get Event targetType
        if (SceneManagement.isShowEventPopup && !string.IsNullOrEmpty(SceneManagement.EventtargetUser))
            Eventtype = SceneManagement.EventtargetUser.ToLower();

        // Get Notification targetType & match check
        LoginNotification notification = null;

        if (SceneManagement.isShowNotificationAfterLogin &&
            SceneManagement.LatestNotifications != null &&
            SceneManagement.LatestNotifications.Count > 0)
        {
            notification = SceneManagement.LatestNotifications[0];

            if (!string.IsNullOrEmpty(notification.targetType))
                Notificationtype = notification.targetType.ToLower();

            NotificationActive = notification.isActive;

            if (notification.targetUsers != null &&
                notification.targetUsers.Exists(id =>
                    string.Equals(id, userId, StringComparison.OrdinalIgnoreCase)))
            {
                isShow = true;
            }
        }

        //Debug.Log($"EventType: {Eventtype}, NotificationType: {Notificationtype}, UserType: {userType}, IsShow: {isShow}, EventActive: {EventisActive}, NotificationActive: {NotificationActive}");

        bool shouldShowForEvent =
            EventisActive &&
            (string.Equals(Eventtype, userType, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(Eventtype, "all", StringComparison.OrdinalIgnoreCase));

        bool isTargetTypeMatch =
       string.Equals(Notificationtype, userType, StringComparison.OrdinalIgnoreCase) ||
       string.Equals(Notificationtype, "all", StringComparison.OrdinalIgnoreCase);

        bool shouldShowForNotification =
            NotificationActive &&
            isTargetTypeMatch;

        //Debug.Log($"[Notify] ID Match: {isShow}, Type Match: {isTargetTypeMatch}, Final Show: {shouldShowForNotification}");

        if (notification != null && !shouldShowForNotification)
        {
            notification.isActive = false;
            Debug.Log("❌ Notification marked as inactive because userType doesn't match targetType.");
        }


        if (shouldShowForEvent || shouldShowForNotification)
        {
            var btn = Instantiate(MidNightPartBtn_Prefab, MidNightPartBtn_Parent);

            RectTransform rt = btn.GetComponent<RectTransform>();
            // Anchor to bottom-right
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);

            EventAndNotification_Controller enc = btn.GetComponent<EventAndNotification_Controller>();
            Image img = btn.GetComponent<Image>();

            if (img != null)
            {
                if (enc != null)
                {
                    if (shouldShowForNotification)
                    {
                        img.sprite = enc.notificationButtonImage;
                    }
                    else if (shouldShowForEvent)
                    {
                        img.sprite = enc.eventButtonImage;
                    }
                }
            }

            if (SceneManagement.isShowSpinWheel)
            {
                // Position relative to bottom-right
                rt.anchoredPosition = new Vector2(-265f, 38f);
            }
            else
            {
                rt.anchoredPosition = new Vector2(-20f, 10f);

            }
            Sequence heartbeat = DOTween.Sequence();
            heartbeat
                .Append(rt.DOScale(1.08f, 0.25f).SetEase(Ease.OutQuad))     // scale up
                .Join(rt.DORotate(new Vector3(0, 0, 2f), 0.1f))            // rotate right
                .Append(rt.DORotate(new Vector3(0, 0, -2f), 0.1f))          // rotate left
                .Append(rt.DORotate(Vector3.zero, 0.1f))                   // back to center
                .Append(rt.DOScale(1f, 0.2f).SetEase(Ease.InQuad))          // scale normal
                .AppendInterval(2f)
                .SetLoops(-1);


            midnightPartyButton = btn.GetComponent<Button>();
            StartCoroutine(ShowPopupAfterDelay(midnightPartyButton));
        }


    }

    IEnumerator ShowPopupAfterDelay(Button _midnightPartyButton)
    {
        if (_midnightPartyButton != null)
        {
            yield return new WaitForSeconds(3f);
            _midnightPartyButton.GetComponent<EventAndNotification_Controller>()?.ShowPopup();
        }
    }

    //public void SetUserData(string id, string username, int coins, string imgId, string bio)
    public void SetUserData()
    {
        UserID = UserManager.Instance._userGameId;
        Username = UserManager.Instance.Username;
        avatarIndex = UserManager.Instance.avatarIndex;
        Coins = UserManager.Instance.Coins;
        Debug.Log("Coins : " + Coins);
        if (setDefaultAvatarIcon)
        {
            StartRotation();
            AvatarImage = userAvatarDefaultImage;
            setDefaultAvatarIcon = false;
        }
        else
        {
            StopRotation();
            
            if (useAvatarDummyImage)
                UserManager.Instance.AvatarImage = userAvatarDummyImage;

            AvatarImage = UserManager.Instance.AvatarImage;
        }
        Bio = UserManager.Instance.Bio;

        UpdateUserDisplay();
        OnUserDataUpdated.Invoke();
    }

    private void UpdateUserDisplay()
    {
        //Debug.Log("UpdateUserDisplay");
        if (userIdText != null)
        {
            userIdText.richText = true;
            userIdText.text = "<color=#006bff>ID:</color> <color=#11b300>" + UserID + "</color>";
        }

        if (userCoinsText != null)
        {
            string coin = UserManager.Instance.FormatCoins(Coins);
            userCoinsText.text = $"<color=#ffc208>{coin}</color>";
        }

        if (userAvatar != null)
            userAvatar.sprite = AvatarImage;
    }

   
    private void SetupMenuButtons()
    {
        menuToggleBtn.onClick.AddListener(() => ToggleMenuButtonsUI());
        profileButton.onClick.AddListener(() => ProfileClick());
        settingButton.onClick.AddListener(() => ShowPopup(settingsPopup));
        leaderboardButton.onClick.AddListener(() => StartCoroutine(OpenLeaderBoard()));
        changePasswordButton.onClick.AddListener(() => ShowPopup(changePasswordPopup));
        feedbackButton.onClick.AddListener(() => feedbackPopup.gameObject.SetActive(true));

    }

    public void ProfileClick()
    {
        //GlobleSoundManager.Instance.PlaySFX("ProfileClick");
        ToggleAvatarSelectButton?.Invoke();
        ShowPopup(profilePopup);
    }

    IEnumerator OpenLeaderBoard()
    {
        GlobleSoundManager.Instance.PlaySFX("Swipe");
        transform.GetComponent<LeaderboardPanelManager>().RemoveData();
        transform.GetComponent<LeaderboardPanelManager>().ResetRank();
        ShowPopup(leaderboardPopup);
        //Debug.Log("OpenLeaderBoard");

        CasinoUIManager.Instance.ShowErrorCanvas(0, "");
        yield return new WaitForSeconds(2f);
        CasinoUIManager.Instance.ShowErrorCanvas(2, "");

        transform.GetComponent<LeaderboardPanelManager>().ShowLeaderboard();
    }

    public void ShowPopup(GameObject popup)
    {
        GlobleSoundManager.Instance.PlaySFX("Swipe");
        //Debug.Log("Nasir ShowPopup ");
        ToggleMenuButtonsUI();
        CylindricalUIWarpSwipe.isDragable = false;
        popup.SetActive(true);
        //popup.transform.GetChild(0).DOScale(1f, 0.2f);
        DoTweenAnim(TweenType.Panel, popup.transform.GetChild(0).gameObject, 1f, 0.3f);

        PopupShown?.Invoke();
    }

    public void HidePopup(GameObject popup)
    {
        //Debug.Log("Nasir HidePopup ");
        if (isDragable())
            CylindricalUIWarpSwipe.isDragable = true;
        else
            CylindricalUIWarpSwipe.isDragable = false;

        //popup.transform.GetChild(0).DOScale(0f, 0.1f);
        DoTweenAnim(TweenType.Panel, popup.transform.GetChild(0).gameObject, 0, 0.1f);
        popup.SetActive(false);
    }

    private void HideAllPopups()
    {
        //Debug.Log("Nasir HideAllPopups ");
        profilePopup.SetActive(false);
        leaderboardPopup.SetActive(false);
        changePasswordPopup.SetActive(false);
        settingsPopup.SetActive(false);
        feedbackPopup.gameObject.SetActive(false);
        if (isDragable())
            CylindricalUIWarpSwipe.isDragable = true;
        else
            CylindricalUIWarpSwipe.isDragable = false;

    }


    public enum TweenType
    {
        None,
        Button,
        Panel,
        SpinWheel
    }

    public void DoTweenAnim(TweenType type, GameObject obj, float scale, float duration)
    {
        if (obj == null) return;

        obj.transform.DOKill();
        switch (type)
        {
            case TweenType.Button:
                obj.transform.localScale = Vector3.one * scale; // Reset to intended base scale
                obj.transform.DOScale(scale * 0.9f, duration)
                    .SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() =>
                    {
                        obj.transform.DOScale(scale, duration)
                            .SetEase(Ease.OutQuad).SetUpdate(true);
                    });
                break;

            case TweenType.Panel:
                obj.transform.localScale = Vector3.one * 0.5f;

                obj.transform.DOScale(scale, duration * 1.2f)
                    .SetEase(Ease.OutBack).SetUpdate(true);
                break;

            case TweenType.SpinWheel:
                obj.transform.localScale = Vector3.one * 0.5f;
                obj.transform.DOScale(scale, duration * 1.2f)
                    .SetEase(Ease.OutBack).SetUpdate(true);
                break;

            case TweenType.None:
            default:
                break;
        }
    }
    void SpawnEffectAtMousePosition(string inputType)
    {
        Vector3 clickPosition = Input.mousePosition;
        clickPosition.z = 10f; // Distance from camera for screen-to-world conversion

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(clickPosition);

        GameObject effect = Instantiate(clickEffectPrefab, worldPosition, Quaternion.identity);
        GlobleSoundManager.Instance.PlaySFX("Click");
        Destroy(effect, effectDuration);
    }
    public bool isDragable()
    {
        GameCatalogueController gameCatalogueController;

        gameCatalogueController = transform.GetComponent<GameCatalogueController>();
        if (gameCatalogueController != null)
        {
            int count = gameCatalogueController.SpawnedItemsCount();
            if (count <= 8)
                return false;
            else
                return true;
        }
        else
        {
            Debug.LogWarning("nasir_warning gameCatalogueController not found");
            return true;
        }
    }
}



