using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UltimateFireLinkRueRoyaleMiniGameManager : MonoBehaviour
{
    #region Variables
    public static UltimateFireLinkRueRoyaleMiniGameManager Instance;

    [SerializeField] private GameObject miniGameStartFrame;
    [SerializeField] private GameObject miniGameWinFrame;
    public GameObject reSpinsCountText;

    [SerializeField] private TMP_Text reSpinsText;
    private TMP_Text reSpinWinText;

    [SerializeField] private Button startOkButton;
    public bool startPopupConfirmed;

    [SerializeField] private Button endOkButton;
    public bool endPopupConfirmed;

    private bool isMiniGame = false;
    public int reSpinsLeft = 3;
    private bool featureRunning;
    private bool firstReSpin;

    private Coroutine reSpinRoutine;
    private bool cancelRequested;
    public int currentLockedCount = 0;
    [SerializeField] public GameObject Target;
    [SerializeField] public TMP_Text Target_Text;
    public float totalCollectedAmount = 0f;
    public float amount = 0f;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void Start()
    {
        reSpinWinText = miniGameWinFrame.transform.GetChild(0).GetComponent<TMP_Text>();
        startOkButton.onClick.AddListener(OnStartPressed);
        endOkButton.onClick.AddListener(OnEndPressed);
    }
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += CancelReSpins;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= CancelReSpins;
    }
    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartReSpinTransition()
    {
        UltimateFireLinkRueRoyaleSlotMachine.Instance.isMiniGame = true;

        featureRunning = true;
        StartCoroutine(StartReSpinGame());
    }
    [ContextMenu("End")]
    public void EndReSpinTransition()
    {
        StartCoroutine(EndReSpinGame());
    }
    public void InitialReSpinText()
    {
        reSpinsText.text = $"{reSpinsLeft}";
    }

    public void ErrorReSpinReturn()
    {
        reSpinsLeft++;
        UpdateReSpinCount();
    }
    public void StartReSpins()
    {
        if (isMiniGame) return;

        cancelRequested = false;
        isMiniGame = true;
        firstReSpin = true;
        reSpinsLeft = 3;
        totalCollectedAmount = 0f;
        Target_Text.text = "$0.00";
        reSpinRoutine = StartCoroutine(ReSpinLoop()); ;
    }
    #endregion

    #region Game Transition

    private IEnumerator StartReSpinGame()
    {
        //yield return new WaitUntil(() => SaharaRichesUIManager.Instance.winAnimationCompleted);
        //yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted);
        //yield return new WaitUntil(() => SaharaRichesJackpotAnimator.Instance.isJackpotCompleted);

        yield return new WaitForSeconds(1f);
        //ShowBackground();
        //SaharaRichesUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(miniGameStartFrame, 1f, 1f, true);
        yield return new WaitForSeconds(0.5f);

        UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.lockedReelIndexes.Clear();
        if (UltimateFireLinkRueRoyaleSlotMachine.Instance.miniGame1)
        {
            var mini = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance;

            mini.lockedReelIndexes.AddRange(UltimateFireLinkRueRoyaleSlotMachine.Instance.miniGameLockedReels);

            mini.fakeLockedReels = true;

            for (int i = 0; i < mini.reels.Count; i++)
            {
                if (mini.reels[i] == null) continue;

                bool locked = mini.fakeLockedReels && mini.lockedReelIndexes.Contains(i);
                mini.reels[i].ApplyLockVisual(locked);
            }
        }
        Debug.Log("LockedReelIndexes : " + UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.lockedReelIndexes);
        UltimateFireLinkRueRoyaleSlotMachine.Instance.BaseGameReelsObject.SetActive(false);
        UltimateFireLinkRueRoyaleSlotMachine.Instance.MiniGameReelsObject.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        startPopupConfirmed = false;
        yield return new WaitUntil(() => startPopupConfirmed);
        PopupAnimation(miniGameStartFrame, 0f, 1f, false);

        yield return new WaitForSeconds(1f);
        //gameTitle.SetActive(false);
        reSpinsCountText.SetActive(true);
        reSpinsLeft = 3;
        InitialReSpinText();

        UltimateFireLinkRueRoyaleUIManager.Instance.UpdateButtons("Free Spin");
        //SaharaRichesUIManager.Instance.StopMusic("Background");
        //SaharaRichesUIManager.Instance.PlayMusic("FreeSpin_Background");

        StartReSpins();
    }

    private IEnumerator EndReSpinGame()
    {
        //SaharaRichesUIManager.Instance.StopMusic("FreeSpin_Background");
        //SaharaRichesUIManager.Instance.PlayMusic("Background");

        yield return new WaitForSeconds(1.5f);

        reSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        UltimateFireLinkRueRoyaleUIManager.Instance.UpdateButtons("Transition End");

        PopupAnimation(miniGameWinFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);
        UltimateFireLinkRueRoyaleSlotMachine.Instance.MiniGameReelsObject.SetActive(false);
        UltimateFireLinkRueRoyaleSlotMachine.Instance.BaseGameReelsObject.SetActive(true);

        //CashVaultUIManager.Instance.TextAnimation(SaharaRichesSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(2.5f);
        endPopupConfirmed = false;
        yield return new WaitUntil(() => endPopupConfirmed);

        PopupAnimation(miniGameWinFrame, 0f, 1f, false);
        Target.SetActive(false);
        reSpinWinText.text = "0.00";

        //if (SaharaRichesSlotMachine.Instance.freeSpinWinAmount > 0)
        //{
        //    SaharaRichesUIManager.Instance.SetAutoInteractable(false);
        //    SaharaRichesUIManager.Instance.SetSpinInteractable(false);
        //    WinAnimation();
        //}
        //else
        //{
        UltimateFireLinkRueRoyaleUIManager.Instance.UpdateButtons("Free Spin End");
        //}
        ResetMiniGameState();
    }
    private void OnStartPressed()
    {
        startPopupConfirmed = true;
    }
    private void OnEndPressed()
    {
        endPopupConfirmed = true;
    }
    private void WinAnimation()
    {
        if (SaharaRichesSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = SaharaRichesSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = SaharaRichesUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, SaharaRichesSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(SaharaRichesSlotMachine.Instance.UpdateGameCoin), 1f);
            SaharaRichesUIManager.Instance.UpdateButtons("Stop");

        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    #endregion

    #region ReSpin
    public void UpdateReSpinCount()
    {
        if (reSpinsText != null)
            reSpinsText.text = reSpinsLeft.ToString();
    }

    private IEnumerator ReSpinLoop()
    {
        yield return new WaitForSeconds(1f);
        currentLockedCount = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.lockedReelIndexes.Count;
        while (featureRunning && reSpinsLeft > 0)
        {
            if (firstReSpin)
            {
                UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.firstReSpin = false;
                firstReSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.AreAllReelsLocked())
                break;

            int previousLockedCount = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.lockedReelIndexes.Count;
            if (cancelRequested) yield break;
            if (currentLockedCount > previousLockedCount)
            {
                reSpinsLeft = 3;
            }
            else
            {
                reSpinsLeft--;
            }

            UpdateReSpinCount();
            float betAmount = UltimateFireLinkRueRoyaleUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.isSpinAgain);

            currentLockedCount = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.lockedReelIndexes.Count;


            if (cancelRequested) yield break;

            yield return new WaitForSeconds(1f);
            //if (SaharaRichesSlotMachine.Instance.currentSpinResult != null)
            //{
            //    if (SaharaRichesSlotMachine.Instance.GetWinAmount() > 0)
            //    {
            //        yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted);
            //    }
            //}
            //yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isCoinCollectionCompleted);
            //yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isDiamondCollectionCompleted);
            //yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isFreeSpinCollectionCompleted);
            //yield return new WaitUntil(() => SaharaRichesJackpotAnimator.Instance.isJackpotCompleted);

        }

        yield return new WaitForSeconds(1f);
        EndReSpins();
    }

    private void EndReSpins()
    {
        featureRunning = false;
        reSpinsLeft = 0;
        isMiniGame = false;
        UltimateFireLinkRueRoyaleSlotMachine.Instance.isMiniGame = false;

        StartCoroutine(EndReSpinsFlow());
    }
    private IEnumerator EndReSpinsFlow()
    {
        yield return StartCoroutine(CollectSpheresToTarget());

        yield return new WaitForSeconds(1.5f);
        EndReSpinTransition();
    }
    private void CancelReSpins()
    {
        if (!isMiniGame) return;

        cancelRequested = true;

        if (reSpinRoutine != null)
        {
            StopCoroutine(reSpinRoutine);
            reSpinRoutine = null;
        }
        UltimateFireLinkRueRoyaleUIManager.Instance.UpdateButtons("Free Spin End");
    }
    #endregion

    #region Sphere Amount Collect
    public HashSet<int> reelsLocked;
    private IEnumerator CollectSpheresToTarget()
    {
        var mini = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance;
        if (mini == null || mini.reels == null) yield break;

        if (Target != null) Target.SetActive(true);

        SortedSet<int> reelsLockedSorted = new SortedSet<int>(mini.lockedReelIndexes);

        reelsLocked = new HashSet<int>(reelsLockedSorted);

        foreach (int reelIndex in reelsLocked)
        {
            if (reelIndex < 0 || reelIndex >= mini.reels.Count)
                continue;

            var reel = mini.reels[reelIndex];
            if (reel == null || reel.slots == null || reel.slots.Count <= 1)
                continue;

            var slot = reel.slots[1];
            if (slot == null)
                continue;

            if (slot.slotType != UltimateFireLinkRueRoyaleMiniGameSlotType.Mini)
                continue;

            amount = slot.GetSphereAmount();

            if (Target_Text != null)
                slot.MoveSphereAmount(Target.transform.position, amount);

            totalCollectedAmount += amount;

            if (Target_Text != null)
                Target_Text.text = $"${totalCollectedAmount:F2}";

            yield return new WaitForSeconds(1.5f);
        }
    }
    #endregion

    #region Reset States
    private void ResetMiniGameState()
    {
        // flow flags
        isMiniGame = false;
        featureRunning = false;
        cancelRequested = false;
        firstReSpin = false;

        // counters
        reSpinsLeft = 3;
        currentLockedCount = 0;
        totalCollectedAmount = 0f;
        amount = 0f;

        // UI
        if (Target != null)
            Target.SetActive(false);

        if (Target_Text != null)
            Target_Text.text = "$0.00";

        if (reSpinsText != null)
            reSpinsText.text = "0";

        // coroutines
        if (reSpinRoutine != null)
        {
            StopCoroutine(reSpinRoutine);
            reSpinRoutine = null;
        }

        // reset mini slot machine state
        var mini = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance;
        if (mini != null)
        {
            mini.fakeLockedReels = false;
            mini.lockedReelIndexes.Clear();
            mini.firstReSpin = false;
            mini.isSpinAgain = false;
            mini.InSpin = false;
        }
        foreach (var reel in UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.reels)
        {
            if (reel == null || reel.slots == null) continue;

            foreach (var slot in reel.slots)
            {
                if (slot != null)
                    slot.ResetSlotState();
            }

            reel.ApplyLockVisual(false);
        }
    }
    #endregion
}