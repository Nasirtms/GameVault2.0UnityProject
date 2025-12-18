using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GoldenDragonMiniGame : MonoBehaviour
{
    #region Variables

    public static GoldenDragonMiniGame Instance;

    [Header("UI")]
    [SerializeField] private GameObject miniGamePopup;
    [SerializeField] private Button smallerButton;
    [SerializeField] private Button equalButton;
    [SerializeField] private Button biggerButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private TMP_Text betAmount;
    [SerializeField] private TMP_Text WinText;
    [SerializeField] private RectTransform cardImage;
    [SerializeField] private RectTransform backImage;


    [SerializeField] private GoldenDragonMiniGameCoinUpdateResposne currentSpinResponse;
    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 1f;
    public float animationPause = 0.5f;

    public List<CardData> cards = new List<CardData>();

    public float mainGameWinAmount;
    public float currentWinAmount;

    private bool canChoose = false;
    public enum Choice { Small, Seven, Big }

    public event Action<float> OnMiniGameBalanceReceived;
    private bool serverResponseReceived = false;
    private bool collectPressed = false;
    private float pendingBalance = 0f;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    public void Start()
    {
        smallerButton.onClick.AddListener(() => OnChoiceButtonPressed(Choice.Small));
        equalButton.onClick.AddListener(() => OnChoiceButtonPressed(Choice.Seven));
        biggerButton.onClick.AddListener(() => OnChoiceButtonPressed(Choice.Big));
        //collectButton.onClick.AddListener(() => EndMiniGameTransition());

        collectButton.onClick.AddListener(OnCollectPressed);

        OnMiniGameBalanceReceived += HandleMiniGameBalance;
        ResetCardViews();
    }

    #endregion

    #region Mini Game

    public void StartMiniGame(float betAmount)
    {
        UpdateBetAmount(betAmount);
        currentWinAmount = 0f;
        GoldenDragonSlotMachine.Instance.isMiniGame = true;
        StartCoroutine(StartMiniGameTransition());
    }

    private IEnumerator StartMiniGameTransition()
    {
        PopupAnimation(miniGamePopup, 1f, 1f, true);

        yield return new WaitForSeconds(0.3f);

        canChoose = true;
        SetButtonsInteractable(true);
    }

    private void EndMiniGameTransition()
    {

        //GameBetServices.Instance.UpdateCoins(currentSpinResponse.newBalance);

        //GoldenDragonSlotMachine.Instance.isMiniGame = false;
        PopupAnimation(miniGamePopup, 0f, 1f, false);

        ResetCardViews();
        GoldenDragonUIManager.Instance.UpdateButtons("Stop");
        collectButtonTween?.Kill();
        collectButton.transform.localScale = Vector3.one;

    }

    #endregion

    #region Collect Flow
    private void OnCollectPressed()
    {
        collectPressed = true;

        // stop animation
        collectButtonTween?.Kill();
        collectButton.transform.localScale = Vector3.one;

        TryApplyCoinUpdate();
    }
    private void TryApplyCoinUpdate()
    {
        // LOSE → update coins as soon as server response arrives
        if (!IsWinningRound() && serverResponseReceived)
        {
            FinalizeBalanceUpdate();
            return;
        }

        // WIN → update only if BOTH server + collect are done
        if (IsWinningRound() && collectPressed && serverResponseReceived)
        {
            FinalizeBalanceUpdate();
        }
    }

    private void FinalizeBalanceUpdate()
    {
        Debug.Log("🔥 Final coin update: " + pendingBalance);

        GameBetServices.Instance.UpdateCoins(pendingBalance);

        EndMiniGameTransition();

        // reset states
        serverResponseReceived = false;
        collectPressed = false;
        pendingBalance = 0f;

        GoldenDragonSlotMachine.Instance.isMiniGame = false;
    }
    #endregion


    #region UI / Coins

    public void UpdateBetAmount(float winAmount)
    {
        mainGameWinAmount = winAmount;
        betAmount.text = $"{winAmount:F2}";
    }
    public void UpdateWinAmount(float winAmount)
    {
        currentWinAmount = winAmount;
        betAmount.text = currentWinAmount.ToString("0.00");
        GoldenDragonUIManager.Instance.winAmount.text = currentWinAmount.ToString("0.00");
    }

    #endregion

    #region Game
    private bool lastRoundWasWin = false;

    private bool IsWinningRound() => lastRoundWasWin;
    private CardCategory GetCategoryForChoice(Choice choice)
    {
        switch (choice)
        {
            case Choice.Small: return CardCategory.LessThan6;
            case Choice.Seven: return CardCategory.Seven;
            case Choice.Big: return CardCategory.GreaterThan8;
        }
        return CardCategory.LessThan6;
    }

    private int GetMultiplier(Choice choice)
    {
        return choice == Choice.Seven ? 8 : 2;
    }
    private void OnChoiceButtonPressed(Choice choice)
    {
        if (!canChoose) return;

        canChoose = false;
        SetButtonsInteractable(false);

        StartCoroutine(ResolveRoundRoutine(choice));
    }

    public bool isFake;
    public bool isBonus;
    public int multiplier;
    private IEnumerator ResolveRoundRoutine(Choice choice)
    {
        yield return new WaitForSeconds(0.1f);

        var spinResult = GoldenDragonSlotMachine.Instance.currentSpinResult;
        isBonus = spinResult.goldenDragonIsBonus;
        multiplier = spinResult.goldenDragonIsBonusMultiplier;

        if (isFake)
        {
            isBonus = true;
            multiplier = 8;
        }
        CardCategory chosenCategory = GetCategoryForChoice(choice);
        int expectedMultiplier = GetMultiplier(choice);


        //bool isWin = isBonus && multiplier == expectedMultiplier;
        lastRoundWasWin = (isBonus && multiplier == expectedMultiplier);
        CardData chosenCard = GetRandomCard(lastRoundWasWin, GetCategoryForChoice(choice));

        backImage.GetComponent<Image>().sprite = chosenCard.image;

        yield return StartCoroutine(FlipCardRoutine());

        StartCoroutine(SendMiniGameCoinUpdate());

        if (lastRoundWasWin)
        {
            PlayWinTextAnimation();
            UpdateWinAmount(currentWinAmount);

            collectButton.interactable = true;
            AnimateCollectButton();
        }
        else
        {
            WinText.gameObject.SetActive(false);
            betAmount.text = "0.00";
            GoldenDragonUIManager.Instance.winAmount.text = "0.00";
            GoldenDragonPaylineController.Instance.ClearPaylineData();
            Invoke(nameof(EndMiniGameTransition), 2f);
        }
    }
    private CardData GetRandomCard(bool isWin, CardCategory chosenCategory)
    {
        List<CardData> pool = cards.FindAll(
            c => isWin ? c.category == chosenCategory : c.category != chosenCategory
        );

        CardData chosenCard = pool.Count > 0 ? pool[Random.Range(0, pool.Count)]
                                         : cards[Random.Range(0, cards.Count)];

        currentWinAmount = isWin ? mainGameWinAmount * multiplier : 0f;

        return chosenCard;
    }
    public IEnumerator FlipCardRoutine()
    {
        ResetCardViews();

        bool slotActive = true;

        Sequence seq = DOTween.Sequence();

        seq.Append(cardImage.DOScale(Vector3.one * scaleAmount, animationDuration));
        seq.Join(backImage.DOScale(Vector3.one * scaleAmount, animationDuration));

        seq.AppendInterval(animationPause);

        seq.AppendCallback(() =>
        {
            RectTransform active = slotActive ? cardImage : backImage;
            RectTransform inactive = slotActive ? backImage : cardImage;

            active.DORotate(new Vector3(0, 90, 0), animationPause).OnComplete(() =>
            {
                active.gameObject.SetActive(false);
                inactive.gameObject.SetActive(true);
                inactive.localEulerAngles = new Vector3(0, 90, 0); // start flipped
                inactive.DORotate(Vector3.zero, animationPause);
            });
        });

        seq.AppendInterval(animationPause);

        seq.Append(cardImage.DOScale(Vector3.one, animationDuration));
        seq.Join(backImage.DOScale(Vector3.one, animationDuration));

        seq.AppendCallback(() =>
        {
            slotActive = !slotActive;
        });

        yield return seq.WaitForCompletion();
    }

    #endregion

    #region Animation

    private void HandleMiniGameBalance(float newBalance)
    {
        Debug.Log("🔥 Updating game balance AFTER server response: " + newBalance);
        GameBetServices.Instance.UpdateCoins(newBalance);
    }
    private Tween collectButtonTween;
    private void AnimateCollectButton()
    {
        // Kill previous tween if still running
        collectButtonTween?.Kill();

        // Reset initial scale
        collectButton.transform.localScale = Vector3.one;

        // Create a continuous pulsing animation
        collectButtonTween = collectButton.transform
        .DOScale(1.08f, 1f)          // small scale increase, slow duration
        .SetEase(Ease.InOutSine)       // very smooth ease
        .SetLoops(-1, LoopType.Yoyo);
    }

    private void PlayWinTextAnimation()
    {
        WinText.gameObject.SetActive(true);

        var rt = WinText.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 300f);

        Color col = WinText.color;
        col.a = 1f;
        WinText.color = col;

        DOTween.Sequence()
            .Append(rt.DOAnchorPos(new Vector2(rt.anchoredPosition.x, 0f), 0.6f).SetEase(Ease.OutBounce))
            .AppendInterval(0.3f)
            .Append(WinText.DOFade(0f, 0.35f))
            .OnComplete(() => WinText.gameObject.SetActive(false));
    }

    private void SetButtonsInteractable(bool state)
    {
        smallerButton.interactable = state;
        equalButton.interactable = state;
        biggerButton.interactable = state;
        collectButton.interactable = state;
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.8f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    private void ResetCardViews()
    {
        cardImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);

        cardImage.localScale = Vector3.one;
        backImage.localScale = Vector3.one;
        cardImage.localEulerAngles = Vector3.zero;
        backImage.localEulerAngles = Vector3.zero;
    }

    #endregion

    #region API Request
    public IEnumerator SendMiniGameCoinUpdate()
    {
        string url = ApiEndpoints.GoldenDragonMiniGameCoinUpdate;

        GoldenDragonMiniGameCoinUpdateRequest req = new GoldenDragonMiniGameCoinUpdateRequest
        {
            gameId = SceneManagement.currentGameID,
            NewWinAmount = currentWinAmount
        };

        string jsonData = JsonConvert.SerializeObject(req);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            Debug.Log("📦 LovKumar Request Body: " + jsonData);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            if (www.responseCode == 401)
            {
                Debug.Log("LovKumar Spin API failed 1: " + www.error);
                yield return null;
            }
            if (www.responseCode == 200)
            {
                string responsejson = www.downloadHandler.text;
                currentSpinResponse = JsonConvert.DeserializeObject<GoldenDragonMiniGameCoinUpdateResposne>(responsejson);

                pendingBalance = currentSpinResponse.newBalance;

                serverResponseReceived = true;

                TryApplyCoinUpdate();

                //Debug.Log("Server newBalance = " + currentSpinResponse.newBalance);

                //OnMiniGameBalanceReceived?.Invoke(currentSpinResponse.newBalance);
            }
            //if (www.responseCode == 200)
            //{
            //    string responsejson = www.downloadHandler.text;
            //    Debug.Log("LovKumar Spin API called responsejson : " + responsejson);
            //    currentSpinResponse = JsonConvert.DeserializeObject<GoldenDragonMiniGameCoinUpdateResposne>(responsejson);
            //    Debug.Log("LovKumar Spin API called currentSpinResponse: " + currentSpinResponse);
            //    Debug.Log("LovKumar Spin API called newBalance : " + currentSpinResponse.newBalance);
            //}

        }
    }
    #endregion
    
}
[System.Serializable]
public class CardData
{
    public CardCategory category;
    public string name;
    public Sprite image;
}
public enum CardCategory
{
    LessThan6,
    Seven,
    GreaterThan8
}
[Serializable]
public class GoldenDragonMiniGameCoinUpdateResposne
{
    public bool success;
    public string message;
    public float lastLogWinAmount;
    public float newBetAmount;
    public float newWinAmount;
    public float newBalance;
}

[Serializable]
public class GoldenDragonMiniGameCoinUpdateRequest
{
    public string gameId;
    public float NewWinAmount;
}