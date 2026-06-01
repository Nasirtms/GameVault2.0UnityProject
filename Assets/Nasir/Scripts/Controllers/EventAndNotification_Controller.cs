using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SerializableClasses;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Transform = UnityEngine.Transform;

public class EventAndNotification_Controller : MonoBehaviour
{
    enum notificationType { None, text, image }
    private notificationType currentType = notificationType.None;

    [SerializeField] private Button eventBtn;
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private GameObject TextNotificationPanel;

    public Sprite eventButtonImage;
    public Sprite notificationButtonImage;

    public float delayBeforeFirstPopup = 1f;
    public float delayBetweenPopups = 1f;

    [SerializeField] private GameObject popupNotification;
    [SerializeField] private GameObject popupEvent;
    [SerializeField] private Sprite notificationSprite;
    [SerializeField] private bool inProcess;

    GameObject ActivePopup;

    //--------- Text Notification Variables --------------- \\
    private string Notification_time;
    private string Notification_Endtime;
    private string Notification_Massage;


    void Start()
    {
        if (eventBtn != null)
            eventBtn.onClick.AddListener(ShowPopup);
    }

    public void ShowPopup()
    {
        if (inProcess) return;
        inProcess = true;
        StartCoroutine(ShowPopupsSequence());
    }

    private IEnumerator ShowPopupsSequence()
    {
        yield return new WaitForSeconds(0.2f);
        if (SceneManagement.isShowNotificationAfterLogin)
        {
            if (SceneManagement.LatestNotifications.Count > 0)
            {
                if (SceneManagement.LatestNotifications[0].isActive)
                {
                    yield return SetupNotificationPopup();
                    yield return new WaitForSeconds(delayBeforeFirstPopup);
                    yield return ShowPopupWithAnimation(popupNotification);
                    inProcess = false;
                }
            }
        }
        if (SceneManagement.isShowEventPopup)
        {
            if (SceneManagement.eventIsActive)
            {
                yield return SetupEventPopup();
                yield return new WaitForSeconds(delayBetweenPopups);
                yield return ShowPopupWithAnimation(popupEvent);
            }
        }

    }

    private IEnumerator SetupNotificationPopup()
    {
        if (SceneManagement.LatestNotifications?.Count == 0)
        {
            yield return null;
        }
        if (SceneManagement.LatestNotifications?.Count > 0)
        {
            var data = SceneManagement.LatestNotifications[0];

            Debug.Log("type : " + data.type);
            switch (data.type)
            {
                case "image":
                    if (!string.IsNullOrEmpty(data.imageUrl))
                        yield return StartCoroutine(DownloadNotificationImage(data.imageUrl));

                    currentType = notificationType.image;
                    if (popupNotification == null)
                    {
                        popupNotification = Instantiate(notificationPanel, MainMenuUIManager.Instance.PopupParent);
                        popupNotification.name = "notification";
                        ActivePopup = popupNotification;
                    }
                    var closeBtn = popupNotification.transform.GetChild(0).GetChild(1).GetComponent<Button>();
                    closeBtn.onClick.AddListener(() => HidePopup(popupNotification));

                    if (notificationSprite != null)
                        popupNotification.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = notificationSprite;
                    break;
                case "text":
                    SetMidNightData(data);
                    break;
            }
        }
    }

    void SetMidNightData(LoginNotification data)
    {
        currentType = notificationType.text;

        if (popupNotification == null)
        {
            popupNotification = Instantiate(TextNotificationPanel, MainMenuUIManager.Instance.PopupParent);
            popupNotification.name = "notification";
            ActivePopup = popupNotification;
        }

        Transform page1 = popupNotification.transform.GetChild(0).GetChild(1);
        Transform page2 = popupNotification.transform.GetChild(0).GetChild(2);
        Transform page3 = popupNotification.transform.GetChild(0).GetChild(3);

        // ✅ Show only first page initially
        page1.gameObject.SetActive(true);
        page2.gameObject.SetActive(false);
        page3.gameObject.SetActive(false);

        #region Add Button Listener
        var acceptBtnPage1 = page1.GetChild(2).GetComponent<Button>();
        var closeBtn1 = page1.GetChild(1).GetComponent<Button>();

        var closeBtn2 = page2.GetChild(0).GetComponent<Button>();
        var RulesBtnPage2 = page2.GetChild(1).GetComponent<Button>(); 

        var closeBtn3 = page3.GetChild(0).GetComponent<Button>();

        acceptBtnPage1.onClick.AddListener(() =>
        {
            page1.gameObject.SetActive(false);
            page2.gameObject.SetActive(true);
        });

        RulesBtnPage2.onClick.AddListener(() =>
        {
            page3.gameObject.SetActive(true);
        });

        closeBtn1.onClick.AddListener(() => HidePopup(popupNotification));
        closeBtn2.onClick.AddListener(() => HidePopup(popupNotification));
        closeBtn3.onClick.AddListener(() => HidePopup(popupNotification));
        #endregion

        #region Set Data
        TMP_Text page1TextBox = page1.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
        page1TextBox.text = $"Do you want to participate in a midnight party";

        TMP_Text page2TextBox1 = page2.GetChild(2).GetChild(0).GetComponent<TMP_Text>();
        TMP_Text page2TextBox2 = page2.GetChild(3).GetChild(0).GetComponent<TMP_Text>();
        page2TextBox1.text = $"{data.title}";
        page2TextBox2.text = $"{data.message}";

        TMP_Text page3TextBox = page3.GetChild(1).GetChild(0).GetComponentInChildren<TMP_Text>(); 
        page3TextBox.text = $"{data.message}";
        #endregion
    }

    private IEnumerator SetupEventPopup()
    {
        if (popupEvent == null)
        {
            popupEvent = Instantiate(eventPanel, MainMenuUIManager.Instance.PopupParent);
            popupEvent.name = "event";
            ActivePopup = popupEvent;
            var closeBtn = popupEvent.transform.GetChild(0).GetChild(0).GetComponent<Button>();
            closeBtn.onClick.AddListener(() => HidePopup(popupEvent));

            SetEventText(popupEvent);
        }

        yield return null;
    }

    private void SetEventText(GameObject popup)
    {
        var content = popup.transform.GetChild(0);
        var text = content.transform.GetChild(2);
        content.transform.GetChild(1).GetComponent<TMP_Text>().text = SceneManagement.eventHeading;
        text.Find("massage").GetComponent<TMP_Text>().text = SceneManagement.evntMassage;
        text.Find("bottom").GetComponent<TMP_Text>().text = SceneManagement.evntBottom;
    }

    private IEnumerator ShowPopupWithAnimation(GameObject popup)
    {

        if (currentType == notificationType.text)
        {
            popupNotification.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            popupNotification.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
            popupNotification.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);
        }

        popup.SetActive(true);

        var mainMenu = MainMenuUIManager.Instance;
        if (mainMenu != null)
            mainMenu.DoTweenAnim(MainMenuUIManager.TweenType.Panel, popup.transform.GetChild(0).gameObject, 1f, 0.3f);

        CylindricalUIWarpSwipe.isDragable = false;

        yield return new WaitUntil(() => !popup.activeSelf);

        if (MainMenuUIManager.Instance.isDragable())
            CylindricalUIWarpSwipe.isDragable = true;
        else
            CylindricalUIWarpSwipe.isDragable = false;

    }

    private void HidePopup(GameObject popup)
    {
        var mainMenu = MainMenuUIManager.Instance;

        // Step 1: Apply state changes based on what popup was shown
        switch (popup.name.ToLower())
        {
            case "notification":
                if (SceneManagement.LatestNotifications != null &&
                    SceneManagement.LatestNotifications.Count > 0)
                {
                    var notification = SceneManagement.LatestNotifications[0];
                    if (notification.showOnce)
                    {
                        notification.isActive = false;
                        Debug.Log("🔔 Notification marked inactive after showing once.");
                    }
                }
                break;

            case "event":
                if (SceneManagement.isShowEventPopup)
                {
                    if (SceneManagement.eventIsActive && SceneManagement.EventshowOnce)
                    {
                        SceneManagement.eventIsActive = false;
                        SceneManagement.EventshowOnce = false;
                        Debug.Log("📅 Event marked inactive after showing once.");
                    }
                }
                else
                {
                    SceneManagement.eventIsActive = false;
                    SceneManagement.EventshowOnce = false;
                    Debug.Log("📅 Event marked inactive after showing once. 2");
                }
                break;
        }

        // Step 2: Recalculate active states
        bool hasActiveNotification =
            SceneManagement.LatestNotifications != null &&
            SceneManagement.LatestNotifications.Count > 0 &&
            SceneManagement.LatestNotifications[0].isActive;

        bool hasActiveEvent =
            SceneManagement.eventIsActive;

        // Step 3: Hide this parent object if there's nothing left to show
        if (!hasActiveNotification && !hasActiveEvent)
        {
            gameObject.SetActive(false);
            Debug.Log("❌ No active notification or event. Hiding container object.");
        }

        // Step 4: Animate and disable popup
        if (mainMenu != null)
        {
            var popupContent = popup.transform.GetChild(0).gameObject;

            popupContent.transform.DOScale(0f, 0.1f).OnComplete(() =>
            {
                popup.SetActive(false);
            });
        }
        else
        {
            popup.SetActive(false);
        }

        if (ActivePopup != null) { MainMenuUIManager.Instance.HidePopup(ActivePopup); }
    }

    private IEnumerator DownloadNotificationImage(string url)
    {
        url = url.Replace("gamesimages//", "gamesimages/");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                notificationSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                Debug.LogError($"❌ Failed to download image: {request.error}");
            }
        }
    }

    private void OnDestroy()
    {
        currentType = notificationType.None;
        if (popupNotification != null)
        {
            Destroy(popupNotification.gameObject);
        }
    }

}
