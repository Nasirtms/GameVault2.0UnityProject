using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class GoldRushGusMiniGameController : MonoBehaviour
{
    #region Variables

    public static GoldRushGusMiniGameController Instance;

    [Header("UI")]
    [SerializeField] private GameObject miniGamePopup;
    [SerializeField] private GameObject miniGameBG;
    private CanvasGroup bgGroup;

    [SerializeField] private List<GoldRushGusMiniGameCoinScript> coins = new List<GoldRushGusMiniGameCoinScript>(7);
    [SerializeField] private Button collectButton;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text WinText;

    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite lossSprite;

    public TMP_Text betAmount;
    public int currentMultiplier = 15;
    private bool canPick;
    private bool ended;

    [Header("Flip Animation")]
    [SerializeField] private float scaleAmount = 1.15f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private float flipHalfDuration = 0.5f;
    [SerializeField] private float smallPause = 0.03f;
    public float zoomOutScale = 0.3f;

    private CanvasGroup popupGroup;

    private int targetMiniGameMultiplier;   // from backend
    private int maxSafeWins;                // how many win coins allowed
    private int winsRevealed;
    private readonly Dictionary<int, int> _multiplierMap = new() { { 0, 15 }, { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 5, 60 }, { 6, 100 } };

    private int currentMultiplierIndex;
    private bool collectPressed = false;
    SerializableClasses.GoldRushGusMiniGameCoinUpdateResposne currentSpinResponse;
    public event Action<float> OnMiniGameBalanceReceived;
    private float currentNewBalance = 0f;
    private float currentWinAmount = 0f;
    private bool serverResponseReceived = false;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        popupGroup = miniGamePopup.transform.GetComponent<CanvasGroup>();
        bgGroup = miniGameBG.GetComponent<CanvasGroup>();
    }
    private void Start()
    {
        for (int i = 0; i < coins.Count; i++)
        {
            int idx = i;

            if (coins[idx].button == null)
                coins[idx].button = coins[idx].GetComponent<Button>();

            coins[idx].button.onClick.AddListener(() => OnCoinPressed(idx));
        }

        collectButton.onClick.AddListener(OnCollectPressed);
        ResetMiniGame();
        OnMiniGameBalanceReceived += HandleMiniGameBalance;
    }

    #endregion

    #region Public References

    public void StartMiniGameTransition()
    {
        GoldRushGusSlotMachine.Instance.isMiniGame = true;
        GoldRushGusSlotMachine.Instance.isMiniGameRunning = true;
        ResetMiniGame();
        StartCoroutine(StartMiniGame());
    }
    public IEnumerator StartMiniGame()
    {
        yield return new WaitUntil(() => GoldRushGusUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => GoldRushGusUIManager.Instance.isTreasureAnimationCompleted);
        yield return new WaitUntil(() => GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted);

        GoldRushGusPaylineController.Instance.StopPaylines();
        GoldRushGusPaylineController.Instance.ClearPaylineData();

        GoldRushGusUIManager.Instance.StopMusic("BG");
        GoldRushGusUIManager.Instance.PlaySound("MiniGamePopup");

        ShowMiniGameBG();

        miniGamePopup.SetActive(true);
        miniGamePopup.transform.localScale = Vector3.one * 0.85f;
        miniGamePopup.transform
            .DOScale(1f, 1f)
            .SetEase(Ease.OutBack);

        GoldRushGusUIManager.Instance.PlayMusic("BG");
        yield return new WaitUntil(() => UserPressedConfirm());

        HideMiniGameBG();

        canPick = true;
        ended = false;
       
    }
    private void ResetMiniGame()
    {
        canPick = false;
        ended = false;
        serverResponseReceived = false;
        currentNewBalance = 0f;
        currentWinAmount = 0f;
        int backendIndex = GoldRushGusSlotMachine.Instance.miniGameMultiplier; 
        currentMultiplierIndex = backendIndex;
        //Backend Result
        targetMiniGameMultiplier = (_multiplierMap.TryGetValue(backendIndex, out int value) ? value : 10);
        maxSafeWins = GetMaxSafeWins(targetMiniGameMultiplier);
        winsRevealed = 0;

        for (int i = 0; i < coins.Count; i++)
            coins[i].ResetView();

        collectButton.interactable = true;
        currentMultiplier = 15;
        UpdateMultiplierText();
    }
    private void OnCoinPressed(int index)
    {
        if (!canPick || ended) return;
        if (index < 0 || index >= coins.Count) return;
        if (coins[index].revealed) return;

        canPick = false;
        SetAllCoinsInteractable(false);

        StartCoroutine(ResolveRoundRoutine(index));
    }

    private void OnCollectPressed()
    {
        if (ended) return;

        collectPressed = true;
        ended = true;

        if (winsRevealed == 0)
        {
            currentMultiplier = targetMiniGameMultiplier;
            currentMultiplierIndex = 0;
        }
        else
        {
            currentMultiplierIndex = GetIndexFromMultiplier(currentMultiplier);
            UnityEngine.Debug.Log("LovKumar currentMultiplierIndex : " + currentMultiplierIndex);
        }

        EndMiniGame();
    }
    private void EndMiniGame()
    {
        if (_endFlow != null) StopCoroutine(_endFlow);
        _endFlow = StartCoroutine(EndMiniGameFlow());
    }
    private Coroutine _endFlow;

    private IEnumerator EndMiniGameFlow()
    {
        canPick = false;
        SetAllCoinsInteractable(false);
        yield return StartCoroutine(SendMiniGameCoinUpdate());

        if (serverResponseReceived)
        {
            GoldRushGusUIManager.Instance.PlaySound("MiniGameEnd");
            //GameBetServices.Instance.UpdateCoins(currentNewBalance);
            GameBetServices.Instance.PlayWinAnimation(GoldRushGusSlotMachine.Instance.CurrentBet(), currentWinAmount, currentNewBalance);
            yield return new WaitUntil(() => GoldRushGusUIManager.Instance.winAnimationCompleted);
        }

        GoldRushGusSlotMachine.Instance.isMiniGame = false;
        GoldRushGusSlotMachine.Instance.isMiniGameRunning = false;

        yield return new WaitForSeconds(2f);
        popupGroup.DOFade(0f, 1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                popupGroup.alpha = 1f;
                miniGamePopup.SetActive(false);
            });
    }

    #endregion

    #region Mini Game

    private IEnumerator ResolveRoundRoutine(int clickedIndex)
    {
        GoldRushGusMiniGameCoinScript coin = coins[clickedIndex];
        bool isLose = (winsRevealed >= maxSafeWins);
        coin.SetBackSprite(isLose ? lossSprite : winSprite);

        //Flip 
        yield return StartCoroutine(FlipCoinRoutine(coins[clickedIndex]));

        if (isLose)
        {
            ended = true;
            currentMultiplier = 10;
            currentMultiplierIndex = 1;
            UpdateMultiplierText();
            LockAllCoins();
            yield return new WaitForSeconds(1f);
            EndMiniGame();
            yield break;
        }

        winsRevealed++;
        int add = GetAddForRevealIndex(winsRevealed);

        currentMultiplier += add;
        UpdateMultiplierText();
        currentMultiplierIndex = GetIndexFromMultiplier(currentMultiplier);
        coin.Lock(); // lock only this coin

        // allow next pick
        SetAllCoinsInteractable(true);
        canPick = true;
    }
    private int GetIndexFromMultiplier(int multiplier)
    {
        foreach (var kv in _multiplierMap)
            if (kv.Value == multiplier) return kv.Key;

        return 1; // fallback to 10
    }
    private IEnumerator FlipCoinRoutine(GoldRushGusMiniGameCoinScript coin)
    {
        GoldRushGusUIManager.Instance.PlaySound("CoinFlip");
        coin.button.interactable = false;

        RectTransform root = coin.root;      // Parent → zoom
        RectTransform visual = coin.visual;    // Child  → flip
        RectTransform front = coin.frontImage;
        RectTransform back = coin.backImage;

        front.gameObject.SetActive(true);
        back.gameObject.SetActive(true);

        root.localScale = Vector3.one;
        visual.localScale = Vector3.one;
        visual.localEulerAngles = Vector3.zero;

        Sequence flipOut = DOTween.Sequence();
        flipOut.Join(
            visual.DOScale(zoomOutScale, flipHalfDuration)
                  .SetEase(Ease.InBack)
        );

        flipOut.Join(
            visual.DORotate(new Vector3(0, 90, 0), flipHalfDuration)
                  .SetEase(Ease.InQuad)
        );

        yield return flipOut.WaitForCompletion();

        front.gameObject.SetActive(false);
        back.gameObject.SetActive(true);

        visual.localEulerAngles = new Vector3(0, 90, 0);
        visual.localScale = Vector3.one * zoomOutScale;

        Sequence flipIn = DOTween.Sequence();
        flipIn.Join(
            visual.DORotate(Vector3.zero, flipHalfDuration)
                  .SetEase(Ease.OutQuad)
        );

        flipIn.Join(
            visual.DOScale(1f, flipHalfDuration)
                  .SetEase(Ease.OutBack)
        );

        yield return flipIn.WaitForCompletion();
    }

    #endregion

    #region API Request
    public IEnumerator SendMiniGameCoinUpdate()
    {
        string url = ApiEndpoints.GoldRushGusCoinGambleGame;

        SerializableClasses.GoldRushGusMiniGameCoinUpdateRequest req = new SerializableClasses.GoldRushGusMiniGameCoinUpdateRequest
        {
            gameId = SceneManagement.currentGameID,
            requestId = Guid.NewGuid().ToString(),
            betAmount = GoldRushGusSlotMachine.Instance.CurrentBet(),
            CoinMultiplier = currentMultiplierIndex
        };

        string jsonData = JsonConvert.SerializeObject(req);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            UnityEngine.Debug.Log("📦 LovKumar Request Body: " + jsonData);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            string responsejson = www.downloadHandler.text;
            currentSpinResponse = JsonConvert.DeserializeObject<SerializableClasses.GoldRushGusMiniGameCoinUpdateResposne>(responsejson);

            UnityEngine.Debug.Log("LovKumar Spin Response: " + JsonConvert.SerializeObject(currentSpinResponse));

            currentNewBalance = currentSpinResponse.newBalance;
            //UnityEngine.Debug.Log("LovKumar Spin currentNewBalance : " + currentNewBalance);
            serverResponseReceived = true;
            currentWinAmount = currentSpinResponse.WinAmount;
            //UnityEngine.Debug.Log("LovKumar Spin currentWinAmount : " + currentWinAmount);

        }
    }
    #endregion

    #region UI Update
    private void HandleMiniGameBalance(float newBalance)
    {
        GameBetServices.Instance.UpdateCoins(newBalance);
    }

    private Tween multiplierTween;
    private void UpdateMultiplierText()
    {
        if (multiplierText == null)
            return;

        multiplierText.text = $"x{currentMultiplier}";

        multiplierTween?.Kill();
        multiplierText.transform.localScale = Vector3.one * 0.5f;
        multiplierTween = multiplierText.transform
            .DOScale(Vector3.one, 0.25f)
            .SetEase(Ease.OutBack);
    }

    private void SetAllCoinsInteractable(bool state)
    {
        for (int i = 0; i < coins.Count; i++)
        {
            if (!coins[i].revealed)
                coins[i].button.interactable = state;
        }
        collectButton.interactable = state;
    }

    private void LockAllCoins()
    {
        for (int i = 0; i < coins.Count; i++)
            coins[i].Lock();
    }
    private bool UserPressedConfirm()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;

        return false;
    }
    private void ShowMiniGameBG()
    {
        miniGameBG.SetActive(true);
        bgGroup.alpha = 0f;

        bgGroup.DOFade(1f, 0.4f)
            .SetEase(Ease.OutQuad);
    }

    private void HideMiniGameBG()
    {
        bgGroup.DOFade(0f, 0.4f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                miniGameBG.SetActive(false);
            });
    }
    #endregion

    #region Helper Functions
    private int GetMaxSafeWins(int backendMultiplier)
    {
        switch (backendMultiplier)
        {
            case 10: return 0;
            case 15: return 0;
            case 20: return 1;
            case 30: return 2;
            case 40: return 3;
            case 60: return 4;
            case 100: return 5;
            default: return 0; 
        }
    }
    private int GetAddForRevealIndex(int revealedCount)
    {
        switch (revealedCount)
        {
            case 0: return 5;
            case 1: return 5;
            case 2: return 10;
            case 3: return 10;
            case 4: return 20;
            case 5: return 40;
            default: return 0;
        }
    }
    #endregion
    
}