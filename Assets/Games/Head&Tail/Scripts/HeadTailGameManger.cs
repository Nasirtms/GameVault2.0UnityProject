using DG.Tweening;
using HeadTailGame;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HeadTailGameManager : MonoBehaviour
{

    #region Variables
    public CurrentPick currentPick;

    [Header("Refs")]
    [SerializeField] private HeadTailCoin3DController coin;
    [SerializeField] private HeadTailBetManager betManager;
    [SerializeField] private HeadTailUIManager ui;

    [Header("Buttons Highlight")]
    [SerializeField] private Image headsHighlightImage;
    [SerializeField] private Image tailsHighlightImage;
    [SerializeField] private float highlightFadeDuration = 0.3f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float preSpinPause = 0.1f;
    [SerializeField, Min(0f)] private float postResultPause = 0.6f;

    [Header("Events (optional hooks)")]
    public UnityEvent OnRoundStart;
    public UnityEvent<bool> OnPlayerWin;

    bool _roundInProgress;

    #endregion

    #region Public References
    void Start()
    {
        // Wire UI events
        UpdateSlotServicesGameName();
        ui.IncreaseBetButton.onClick.AddListener(betManager.Increase);
        ui.DecreaseBetButton.onClick.AddListener(betManager.Decrease);
        betManager.OnBetChanged += ui.SetBetText;

        ui.HeadsButton.onClick.AddListener(() => PlayerChooses(HeadTailCoinFaces.Heads));
        ui.TailsButton.onClick.AddListener(() => PlayerChooses(HeadTailCoinFaces.Tails));
        ui.OnRulesOpened += () =>
        {
            if (coin != null) coin.SetMeshRenderersEnabled(false);
        };
        ui.OnRulesClosed += () =>
        {
            if (coin != null) coin.SetMeshRenderersEnabled(true);
        };
        HeadTailSoundManager.Instance.PlayMusic("Background");
    }
    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        //GameSlotRegistry.Register(sceneName, this);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }

    #endregion

    #region PlayerPick & Spin
    void PlayerChooses(int playerPick)
    {
        if (_roundInProgress || coin.IsSpinning) return;

        float betAmount = betManager.CurrentBet;
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (playerPick == 0) { currentPick = CurrentPick.Head; }
        else if (playerPick == 1) { currentPick = CurrentPick.Tail; }
           
        _roundInProgress = true;

        FadeInImage(playerPick);

        if (playerPick == HeadTailCoinFaces.Tails)
            ui.SetAllButtonsInteractableExcept(ui.TailsButton);
        else
            ui.SetAllButtonsInteractableExcept(ui.HeadsButton);

        HeadTailSoundManager.Instance.PlaySpinMusic("Spin");
        StartCoroutine(RoundRoutine(playerPick));
    }
    public int timeout = 20;
    IEnumerator RoundRoutine(int playerPick)
    {
        OnRoundStart?.Invoke();
        coin.StartLoopSpin();

        yield return new WaitForSeconds(preSpinPause);

        // Fetch API result
        yield return coin.FetchOutcome(timeout);
        if (coin.HasNetworkError)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");

            int fallbackFace = Random.value > 0.5f
                ? HeadTailCoinFaces.Heads
                : HeadTailCoinFaces.Tails;

            bool doneError = false;

            StartCoroutine(coin.StopSpinToResultAfterMinimum(
                fallbackFace, 0f, 1, _ => doneError = true));

            while (!doneError) yield return null;

            SlotSpinService.Instance.AddCurrentBetCoinIntoUserCoin();
            HeadTailSoundManager.Instance.StopSpinMusic("Spin");
            FadeOutImage();
            ui.SetAllButtonsInteractable(true);
            _roundInProgress = false;
            yield break;
        }
        // Decide which face to land on
        int faceToLand = coin.LastIsWin
            ? playerPick
            : (playerPick == HeadTailCoinFaces.Heads ? HeadTailCoinFaces.Tails : HeadTailCoinFaces.Heads);

        // Stop the loop spin *after minimum duration and revolutions*, then land on result
        bool done = false;
        
        StartCoroutine(coin.StopSpinToResultAfterMinimum(faceToLand, 2f, 2, _ =>
        {
            done = true;
        }));

        while (!done) yield return null;

        OnPlayerWin?.Invoke(coin.LastIsWin);

        // Show Win UI if win
        ui.UpdateWinAmount(betManager.CurrentBet, coin.LastIsWin);

        if (coin.LastIsWin)
        {
            double payout = betManager.CurrentBet * 1.98;
            Invoke(nameof(UpdateGameCoin), 1f);
            ui.UpdateCoins();
        }
        yield return new WaitForSeconds(postResultPause);

        FadeOutImage();
        ui.SetAllButtonsInteractable(true);
        _roundInProgress = false;
    }
    #endregion

    #region Helper Functions
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(coin.newBalance);
    }
    private void FadeInImage(int playerPick)
    {
        Image img = playerPick == HeadTailCoinFaces.Heads
            ? headsHighlightImage
            : tailsHighlightImage;

        if (!img) return;

        img.DOKill();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        img.DOFade(1f, highlightFadeDuration);
    }

    private void FadeOutImage()
    {
        if (headsHighlightImage)
        {
            headsHighlightImage.DOKill();
            headsHighlightImage.DOFade(0f, highlightFadeDuration);
        }

        if (tailsHighlightImage)
        {
            tailsHighlightImage.DOKill();
            tailsHighlightImage.DOFade(0f, highlightFadeDuration);
        }
    }
    #endregion
}

[Serializable]
public enum CurrentPick
{
    None,
    Head,
    Tail
}