// Assets/Scripts/Game/GameManager.cs
using HeadTailGame;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


public class HeadTailGameManager : MonoBehaviour
{
    public CurrentPick currentPick;

    [Header("Refs")]
    [SerializeField] private HeadTailCoin3DController coin;
    [SerializeField] private HeadTailBetManager betManager;
    [SerializeField] private HeadTailUIManager ui;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float preSpinPause = 0.1f;
    [SerializeField, Min(0f)] private float postResultPause = 0.6f;

    [Header("Events (optional hooks)")]
    public UnityEvent OnRoundStart;
    public UnityEvent<bool> OnPlayerWin;

    bool _roundInProgress;

    void Start()
    {
        // Wire UI events
        
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

    void PlayerChooses(int playerPick)
    {
        if (_roundInProgress || coin.IsSpinning) return;

        float betAmount = betManager.CurrentBet;
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (playerPick == 0) { currentPick = CurrentPick.Head; }
        else if (playerPick == 1) { currentPick = CurrentPick.Tail; }
           
        _roundInProgress = true;

        if (playerPick == HeadTailCoinFaces.Tails)
            ui.SetAllButtonsInteractableExcept(ui.TailsButton);
        else
            ui.SetAllButtonsInteractableExcept(ui.HeadsButton);

        HeadTailSoundManager.Instance.PlaySpinMusic("Spin");
        StartCoroutine(RoundRoutine(playerPick));
    }

    IEnumerator RoundRoutine(int playerPick)
    {
        OnRoundStart?.Invoke();
        coin.StartLoopSpin();

        yield return new WaitForSeconds(preSpinPause);

        // Fetch API result
        yield return coin.FetchOutcome();

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

        ui.SetAllButtonsInteractable(true);
        _roundInProgress = false;
    }

    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(coin.newBalance);
    }

}

[Serializable]
public enum CurrentPick
{
    None,
    Head,
    Tail
}