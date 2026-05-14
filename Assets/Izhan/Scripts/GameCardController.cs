using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SerializableClasses;

public class GameCardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public GameCatalogueController gameCatalogueController;

    [Header("Addressable")]
    public string addressableLabel; // set by the catalogue when spawning the card
    public string sceneName; // set by the catalogue when spawning the card

    [SerializeField] private string gameID;
    [SerializeField] private string gameTitle;
    [SerializeField] private Toggle targetToggle;
    [SerializeField] private Image imageToDisplay;
    [SerializeField] private Image isHot_Image;
    [SerializeField] private Image isNew_image;
    [SerializeField] private Image isComingSoon_image;
    [SerializeField] private Slider fillSlider;
    [SerializeField] private TMP_Text fillText;
    [SerializeField] private Button button;
    [SerializeField] private FavoriteAnimationSpawner favoriteAnimationSpawner;
    [SerializeField] public float holdThreshold = 0.5f;
    [SerializeField] private float repeatInterval;

    public UnityEvent onLongPress;

    private bool isHeld;
    private Coroutine holdCoroutine;
    private Coroutine _loadRoutine;
    public bool _isFavorite;
    private bool _skipNextToggleEvent;
    private bool _isDownloading;

    public string GetGameID()
    {
        return gameID;
    }

    public void Awake()
    {
        if (targetToggle == null)
            targetToggle = GetComponent<Toggle>();

        targetToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    //public void Update()
    //{
    //    button.interactable = CylindricalUIWarpSwipe.Instance.IsDragging ? false : true;

    //    if (CylindricalUIWarpSwipe.Instance.IsDragging == true)
    //    {
    //        // Stop long-press detection, but keep slider & coroutine running
    //        if (holdCoroutine != null)
    //        {
    //            StopCoroutine(holdCoroutine);
    //            holdCoroutine = null;
    //            isHeld = false;
    //        }
    //    }
    //}

    public void SetGameCardData(string gameTitle, bool isFavorite, bool shinerBool, string gameid)
    {
        gameID = gameid;
        this.gameTitle = gameTitle;

        //if (!string.IsNullOrEmpty(gameTitle))
        //    transform.GetChild(1).GetComponent<TMP_Text>().text = gameTitle;

        if (shinerBool)
            transform.GetChild(0).GetChild(1).GetComponent<PlayGameCardShiner>()?.StartShineAnimation();

        _isFavorite = isFavorite;
        targetToggle.isOn = isFavorite;

        GameItem gi = GetGameItem();
        if (gi != null)
        {
            isHot_Image.gameObject.SetActive(gi.Gametitle.ToLower().Contains("hot"));
            isNew_image.gameObject.SetActive(gi.Gametitle.ToLower().Contains("new"));
            isComingSoon_image.gameObject.SetActive((gi.Gametitle.ToLower().Contains("comingsoon") || gi.Gametitle.ToLower().Contains("coming soon") || gi.Gametitle.ToLower().Contains("coming_soon")));
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_isFavorite == isOn)
            return;

        _isFavorite = isOn;
        HandleToggleClickEffect(isOn);
        Debug.Log("Nasir 1 ");
        if (string.IsNullOrEmpty(UserManager.Instance.UserId) || string.IsNullOrEmpty(gameID))
        {
        Debug.Log("Nasir 2 ");
            Debug.LogWarning("[GameCardController] Missing UserId or GameID. Aborting favorite update.");
            return;
        }
        Debug.Log("Nasir 1 ");

        SetFavoriteButtonInteractable(false);

        if (isOn)
        {
        Debug.Log("Nasir 3 ");
            StartCoroutine(UpdateFavoriteStatus(true));
        }
        else
        {
        Debug.Log("Nasir 4 ");
            StartCoroutine(UpdateFavoriteStatus(false));
        }
        Debug.Log("Nasir 5 ");
    }

    private IEnumerator UpdateFavoriteStatus(bool addToFavorites)
    {
        Debug.Log("Nasir 6 ");
        string url = addToFavorites ? ApiEndpoints.AddGameIntoFavorites : ApiEndpoints.RemoveGameFromFavorites;

        Debug.Log("Nasir 7 ");
        var payload = new SerializableClasses.AddFavoriteRequest
        {
            userId = UserManager.Instance.UserId,
            gameId = gameID
        };

        Debug.Log("Nasir 8 ");
        string jsonBody = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();
            Debug.Log("Nasir 9 ");

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<SerializableClasses.AddFavoriteResponse>(www.downloadHandler.text);
                    Debug.Log($"✅ Favorite Updated: {response.message}");
                    gameCatalogueController.UpdateGameItemList(gameID, addToFavorites, this);

                    CasinoUIManager.Instance.ShowErrorCanvas(1, (addToFavorites ? "Added to " : "Removed from ") + "Favorites");
                }
                catch
                {
                    HandleApiError(!addToFavorites);
                    Debug.LogWarning("⚠️ Response parse error.");
                }
            }
            else if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, url, jsonBody, "POST",() => UpdateFavoriteStatus(addToFavorites));
                yield break;
            }

            else
            {
                HandleApiError(!addToFavorites);
                Debug.LogError($"❌ Favorite Update Failed: {www.error}");
            }
        }
    }


    private void HandleApiError(bool revertToggle)
    {
        CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
        _skipNextToggleEvent = true;
        targetToggle.isOn = revertToggle;
        _isFavorite = revertToggle;
    }

    public void HandleToggleClickEffect(bool isOn)
    {
        if (isOn)
        {
            if (favoriteAnimationSpawner != null && targetToggle != null)
            {
                favoriteAnimationSpawner.TriggerEffect(targetToggle.transform);
            }
        }
    }

    public void SetClickEffectSpawner(FavoriteAnimationSpawner spawner)
    {
        favoriteAnimationSpawner = spawner;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        isHeld = true;
        holdCoroutine = StartCoroutine(HoldThresholdRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHold();
    }

    private IEnumerator HoldThresholdRoutine()
    {
        float timer = 0f;

        while (timer < holdThreshold)
        {
            if (!isHeld)
                yield break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // threshold reached, invoke event (this starts FillThenLoad)
        onLongPress?.Invoke();

        // then repeat at intervals while still held (if you still want repeat behavior)
        while (isHeld)
        {
            yield return new WaitForSecondsRealtime(repeatInterval);
            onLongPress?.Invoke();
        }
    }

    private void CancelHold()
    {
        isHeld = false;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        // If download hasn't started yet we stopped before it began.
        // If a download already started, we do not attempt to cancel the Addressables download here
        // because Addressables download cannot be cleanly cancelled.
        // UI will not update further.
    }

    // signature changed to accept label
    public void OnHoldStart(string sceneName, string gameID, string addressableLabel)
    {
        // if you re-hold before finishing, cancel prior
        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(FillThenLoad(sceneName, gameID, addressableLabel));
    }

    private IEnumerator FillThenLoad(string sceneName, string gameID, string addressableLabel)
    {
        if(holdThreshold < 0.01f)
        {
            Debug.Log("FillThenLoad break");
            yield break;
        }

        // Immediately enter download-progress mode (remove previous fake 3s fill)
        fillSlider.gameObject.SetActive(true);
        fillSlider.value = 0f;
        fillText.gameObject.SetActive(true);
        fillText.text = "0%";

        // Start addressables download on the catalogue and pass a progress callback
        if (gameCatalogueController == null)
        {
            Debug.LogError("[GameCardController] gameCatalogueController is null!");
            yield break;
        }

        // Start the download on the central catalogue; pass a callback to update this card UI
        _loadRoutine = gameCatalogueController.StartCoroutine(
            gameCatalogueController.DownloadAndLaunchGame(addressableLabel, sceneName, gameID, (percent) =>
            {
                try
                {
                    fillSlider.value = percent;
                    string pct = $"{Mathf.RoundToInt(percent * 100)}%";
#if TMP_PRESENT
                    fillText.SetText(pct);
                    fillText.ForceMeshUpdate();
#else
                    fillText.text = pct;
#endif
                }
                catch { }
            })
        );

        // the DownloadAndLaunchGame coroutine will call LoadGame(...) when done, which loads SceneLoader.
        yield break;
    }

    public void SetImage(GameItem game)
    {
        StartCoroutine(nameof(SetImage_Coroutine), game);
    }

    IEnumerator SetImage_Coroutine(GameItem game)
    {
        yield return new WaitUntil(()=> game.gameImage != null);

        imageToDisplay.sprite = game.gameImage;
        imageToDisplay.color = new Color(1, 1, 1, 1);
    }

    public void AddOnClickListener(UnityAction call)
    {
        button.onClick.AddListener(call);
    }

    public GameItem GetGameItem()
    {
        return GameCatalogueController.instance.gameItems.FirstOrDefault(x => x.id == gameID);
    }

    public void ReInitializeGameCardData()
    {
        GameItem gi = GetGameItem();

        if (gi != null)
        {
            gameCatalogueController = GameCatalogueController.instance;
            addressableLabel = gi.addressableLabel;
            SetGameCardData(gi.Gametitle, gi.is_favorite, gi.isGameCardShine, gi.id);
            SetImage(gi);
        }
    }

    public void SetFavoriteButtonInteractable(bool state)
    {
        targetToggle.interactable = state;
    }

    public float scaleMultiplier = .3f;
    public float duration = .4f;
    public int vibrato = 10;
    public float elasticity = 1;

    public void GameCardClicked()
    {
        Vector3 initialScale = transform.localScale;
        button.interactable = false;

        transform.localScale = initialScale;
        transform.DOPunchScale(-initialScale * 0.1f, 0.4f, 9, 1).OnComplete(() =>
        {
            button.interactable = true;
        });
        DOVirtual.DelayedCall(0.25f, () =>
        {
            MainMenu.MainMenuManager.instance.OpenLevel(sceneName, gameID, addressableLabel, gameTitle);
        });
    }
}
