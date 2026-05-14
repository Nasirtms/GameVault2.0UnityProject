using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComeOnCashFreeSpinController : MonoBehaviour
{
    public static ComeOnCashFreeSpinController Instance;

    [SerializeField] private float firstSpinDelay = 1.5f;
    [SerializeField] private float betweenSpinsDelay = 0.6f;
    [SerializeField] public Button tryAgain;
    [SerializeField] public Button takeOffer;

    private int freeSpinDone = 0;
    private int totalFreeSpins = 0;
    public int bonusGameOffer = 0;

    private Coroutine freeSpinRoutine;
    private bool cancelRequested;

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += CancelFreeSpins;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= CancelFreeSpins;
    }

    private void Start()
    {
        tryAgain.onClick.AddListener(TryAgain);
        takeOffer.onClick.AddListener(TakeOffer);
    }
    #endregion


    public void StartFreeSpins()
    {
        if(ComeOnCashSlotMachine.Instance.isFreeGame) return;

        cancelRequested = false;
        ComeOnCashSlotMachine.Instance.isFreeGame = true;
        ComeOnCashSlotMachine.Instance.isFreeGameReady = false;
        ResetFreeSpins();
        UpdateFreeSpins(1);
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void startBonusGame(int[] noteIndexes , float randomDelay)
    {
        ComeOnCashSlotMachine.Instance.isBonusGame = true;
        ComeOnCashSlotMachine.Instance.isBonusGameReady = false;
        ComeOnCashFreeGameTransitionController.Instance.offers.text = $"{bonusGameOffer} of 5 Offers";
        StartCoroutine(BonusGame(noteIndexes, randomDelay));
    }

    private IEnumerator BonusGame(int[] noteIndexes, float delay)
    {
        ComeOnCashFreeGameTransitionController.Instance.StartRandomGlow();
        yield return new WaitForSeconds(delay);
        ComeOnCashFreeGameTransitionController.Instance.stopRandomGlow = true;
        yield return StartCoroutine(ComeOnCashFreeGameTransitionController.Instance.GlowTargetCash(noteIndexes));
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(firstSpinDelay);
        //CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");

        while (freeSpinDone < totalFreeSpins)
        {
            yield return new WaitForSeconds(betweenSpinsDelay);
            if (cancelRequested) yield break;
            float betAmount = ComeOnCashUIManager.Instance.CurrentBet();

            ComeOnCashSlotMachine.Instance.LockedReels.Clear();

            if (!ComeOnCashSlotMachine.Instance.canReel1spin)
            {
                ComeOnCashSlotMachine.Instance.LockedReels.Add(1);
            }

            if (!ComeOnCashSlotMachine.Instance.canReel2spin)
            {
                ComeOnCashSlotMachine.Instance.LockedReels.Add(2);
            }

            if (!ComeOnCashSlotMachine.Instance.canReel3spin)
            {
                ComeOnCashSlotMachine.Instance.LockedReels.Add(3);
            }

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(()=> ComeOnCashSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (ComeOnCashSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ComeOnCashSlotMachine.Instance.isPaylineCompleted);
            }

            freeSpinDone++;
        }

        yield return new WaitForSeconds(1.5f);
        EndFreeSpins();
    }

    public void ResetFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int spins)
    {
        totalFreeSpins += spins;
    }
    
    private void EndFreeSpins()
    {
        ResetFreeSpins();
        if(ComeOnCashUIManager.Instance.reel2LockedBg.activeSelf) ComeOnCashUIManager.Instance.reel2LockedBg.SetActive(false);
        if(ComeOnCashUIManager.Instance.reel3LockedBg.activeSelf) ComeOnCashUIManager.Instance.reel3LockedBg.SetActive(false);
        ComeOnCashUIManager.Instance.betController.UpdateBetUi();
        ComeOnCashSlotMachine.Instance.LockedReels.Clear();
        ComeOnCashSlotMachine.Instance.canReel1spin = true;
        ComeOnCashSlotMachine.Instance.isFreeGame = false;
        ComeOnCashSlotMachine.Instance.decoyFreeSpinBool = false;
        ComeOnCashFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    private void CancelFreeSpins()
    {
        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        ComeOnCashUIManager.Instance.UpdateButtons("Stop");
    }

    private void TryAgain()
    {
        if (bonusGameOffer >= 5) return;
        ComeOnCashUIManager.Instance.PlaySound("Button");
        tryAgain.interactable = false;
        takeOffer.interactable = false;

        ComeOnCashFreeGameTransitionController.Instance.StopGlowOnCash();

        float betAmount = ComeOnCashUIManager.Instance.CurrentBet();
        ComeOnCashSlotMachine.Instance.cashValues.Clear();
        ComeOnCashSlotMachine.Instance.cashIndexes.Clear();
        ComeOnCashSlotMachine.Instance.isBonusRetryRequest = true;
        ComeOnCashSlotMachine.Instance.InSpin = true;

        SlotSpinService.Instance.Spin(betAmount);
    }

    private void TakeOffer()
    {
        ComeOnCashUIManager.Instance.PlaySound("Button");
        tryAgain.interactable = false;
        takeOffer.interactable = false;
        ComeOnCashSlotMachine.Instance.isTakeOffer = true;
        ComeOnCashUIManager.Instance.winAnimationCompleted = false;
        float betAmount = ComeOnCashUIManager.Instance.CurrentBet();
        ComeOnCashSlotMachine.Instance.InSpin = true;
        ComeOnCashSlotMachine.Instance.currentSpinResult = null;
        SlotSpinService.Instance.Spin(betAmount);
    }

    public void EndBonus()
    {
        ComeOnCashSlotMachine.Instance.isBonusGame = false;
        ComeOnCashSlotMachine.Instance.isBonusGameReady = false;
        ComeOnCashSlotMachine.Instance.isTakeOffer = false;
        ComeOnCashUIManager.Instance.winAnimationCompleted = false;
        bonusGameOffer = 0;

        StartCoroutine(ComeOnCashFreeGameTransitionController.Instance.EndBonusGame());
    }

    public void ConsumeOffer()
    {
        bonusGameOffer = Mathf.Min(bonusGameOffer + 1, 5);
        ComeOnCashFreeGameTransitionController.Instance.offers.text = $"{bonusGameOffer} of 5 Offers";
    }

    public void RollbackOffer()
    {
        bonusGameOffer = Mathf.Max(0, bonusGameOffer - 1);
        ComeOnCashFreeGameTransitionController.Instance.offers.text = $"{bonusGameOffer} of 5 Offers";
    }

    public void RestoreBonusButtonsAfterError()
    {
        bool hasOffersLeft = bonusGameOffer < 5;

        tryAgain.interactable = hasOffersLeft;
        takeOffer.interactable = true;
    }
}
