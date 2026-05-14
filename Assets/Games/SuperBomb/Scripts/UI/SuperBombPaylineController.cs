using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static SlotMachine2D;
using static UnityEngine.EventSystems.EventTrigger;

public class SuperBombPaylineController : MonoBehaviour
{
    #region Variables

    public static SuperBombPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<SuperBombPaylineData> paylines;

    // Currently running paylines
    public List<SuperBombPaylineEntry> activePaylines = new List<SuperBombPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;
    [SerializeField] private GameObject paylineHighlighter;
    public bool dontShowComboVfxInSpin = false;
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true
    private bool showPaylineWinOnce = false;
    //public Coroutine paylineRoutine;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<SuperBombPaylineResult> spinResult = new List<SuperBombPaylineResult>();
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void AddPaylineData(SuperBombPaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
            spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        isShowing = false;

        foreach (var entry in activePaylines)
        {
            for (int x = 0; x < SuperBombSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                    int idx = slot.currentResource.index;
                    var slotimage = slot.slots[idx];
                    SetSlotsAlpha(false);
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        entry.payline.paylineImage.gameObject.SetActive(false);
                        slot.StopAnimation(idx);
                        //slot.HidePaylineWin();
                    }
                }
            }
        }
        for (int x = 0; x < SuperBombSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                var idx = slot.currentResource.index;
                if (slot != null && slot.type == SuperBombSlotType.Wild)
                {
                    slot.StopAnimation(idx);
                    //slot.HidePaylineWin();
                }
            }
        }
        spinResult.Clear();
        activePaylines.Clear();
    }
    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<SuperBombPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new SuperBombPaylineEntry(
                    paylineData, result.reelLimit, result.paylineWinAmount, result.isleftToRight
                ));
            }
        }
        if (activePaylines.Count == 0)
        {
            return;
        }
        isShowing = true;

        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void ShowCollectedPaylines(/*int scatterCount*/)
    {
        //resultScatterCount = scatterCount;
        // If no wins, immediately mark complete so auto-spin can proceed
        if (spinResult == null || spinResult.Count == 0)
        {
            SuperBombSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }

        StartPaylineDisplay(spinResult); // <-- your existing private method
    }

    private void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }
    }

    private IEnumerator PlayPaylines()
    {
        if (activePaylines == null || activePaylines.Count == 0)
        {
            SuperBombSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }
        if (!SuperBombAutoSpinController.isAutoSpinning && !SuperBombSlotMachine.Instance.isFreeGame)
        {
            SuperBombSlotMachine.Instance.isPaylineCompleted = true;
        }
        if (activePaylines.Count == 1)
        {
            yield return StartCoroutine(ShowPaylineWinsOnce());
            //paylineRoutine = StartCoroutine(PlayAllPaylines());
            yield return PlayAllPaylines();
        }
        else
        {
            if (SuperBombAutoSpinController.isAutoSpinning)
            {
                yield return StartCoroutine(ShowPaylineWinsOnce());
                //paylineRoutine = StartCoroutine(PlayAllPaylines());
                yield return PlayAllPaylines();
            }
            else
            {
                yield return StartCoroutine(ShowPaylineWinsOnce());
                while (isShowing)
                {
                    //paylineRoutine = StartCoroutine(PlayAllPaylines());
                    yield return PlayAllPaylines();
                }
            }
        }

        SuperBombSlotMachine.Instance.isPaylineCompleted = true;
    }
    public IEnumerator PlayAllPaylines()
    {
        int index = 0;
        int maxIndex = activePaylines.Count;

        if (!dontShowComboVfxInSpin)
        {
            SuperBombUIManager.Instance.ShowComboVfx(maxIndex);
            SuperBombUIManager.Instance.hasShowenComboVfx = true;
        }

        while (true)
        {
            if (activePaylines == null || activePaylines.Count == 0)
            {
                yield return null;
                continue;
            }

            foreach (var pl in activePaylines)
            {
                int totalReels_ = SuperBombSlotMachine.Instance.reels.Count;

                // Figure out start, end, and direction step
                int start_ = pl.isLeftToRight? 0 : totalReels_ - 1;
                int end_ = pl.isLeftToRight ? pl.reelLimit : totalReels_ - pl.reelLimit;
                int step_ = pl.isLeftToRight ? 1 : -1;

                for (int x = start_; pl.isLeftToRight ? x < end_ : x >= end_; x += step_)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (pl.payline.ToMatrix()[x, y] == 1)
                        {
                            if (y + 1 >= SuperBombSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                            if (slot == null) continue;

                            if (pl.payline.ToMatrix()[x, y] == 1)
                            {
                                int idx = slot.currentResource.index;
                                var slotimage = slot.slots[idx];
                                pl.payline.paylineImage.gameObject.SetActive(false);    
                                slot.StopAnimation(idx);
                                SetSlotsAlpha(false);
                                //slot.HidePaylineWin();
                            }
                        }
                    }
                }
            }

            // safe wrap
            if (activePaylines.Count == 0) yield break;
            index = index % activePaylines.Count;

            var currentPayline = activePaylines[index];

            int totalReels = SuperBombSlotMachine.Instance.reels.Count;

            // Figure out start, end, and direction step
            int start = currentPayline.isLeftToRight ? 0 : totalReels - 1;
            int end = currentPayline.isLeftToRight ? currentPayline.reelLimit : totalReels - currentPayline.reelLimit;
            int step = currentPayline.isLeftToRight ? 1 : -1;

            for (int x = start; currentPayline.isLeftToRight ? x < end : x >= end; x += step)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot_ = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot_ == null) continue;

                    int idx = slot_.currentResource.index;
                    var slotimage_ = slot_.slots[idx];

                    if (currentPayline.payline.ToMatrix()[x, y] == 1)
                    {
                        if (y + 1 >= SuperBombSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (currentPayline.payline.ToMatrix()[x, y] == 1)
                        {
                            currentPayline.payline.paylineImage.gameObject.SetActive(true);
                            int idx_ = slot.currentResource.index;
                            var slotimage = slot.slots[idx_];
                            slot.PlayAnimation(idx_, false, false);
                            SetSlotsAlpha(true);
                        }
                        if (x == 2)
                        {
                            //slot.ShowPaylineWin(currentPayline.paylineWinAmount);
                        }
                    }

                }
            }

            yield return new WaitForSeconds(1.8f);

            if (SuperBombAutoSpinController.isAutoSpinning || SuperBombSlotMachine.Instance.isFreeGameReady)
            {
                if (index + 1 == maxIndex)
                {
                    SuperBombSlotMachine.Instance.isPaylineCompleted = true;
                    if (SuperBombSlotMachine.Instance.isFreeGameReady && SuperBombSlotMachine.Instance.freeSpinCount > 0)
                    {
                        ClearPaylineData();
                        SuperBombSlotMachine.Instance.SetSlotAnimationCompleted();
                    }
                    yield break;
                }
            }
            index++;
        }
    }

    public IEnumerator  ConvertTriggeredReelsToWild()
    {
        if (SuperBombSlotMachine.Instance == null)
            yield break;

        yield return new WaitUntil(() => SuperBombUIManager.Instance.winAnimationCompleted);

        var slotMachine = SuperBombSlotMachine.Instance;

        float startDelay = 0.3f; // delay before starting next slot
        float stopDelay = 0.4f;  // delay before stopping previous slot

        for (int i = 0; i < slotMachine.reels.Count; i++)
        {
            var reel = slotMachine.reels[i];
            if (reel == null) continue;

            bool isReelWild =
                slotMachine.triggerReelsMask != null &&
                i < slotMachine.triggerReelsMask.Length &&
                slotMachine.triggerReelsMask[i];

            if (!isReelWild) continue;

            List<SuperBombSlotScript> animatedSlots = new List<SuperBombSlotScript>();

            foreach (var slot in reel.slots)
            {
                if (slot == null) continue;

                var wildSlot = slotMachine.settings.resourcesList
                    .Find(r => r.type == SuperBombSlotType.Wild);

                if(animatedSlots.Count == 0)
                {
                    SuperBombUIManager.Instance.PlaySound("wild");
                }

                slot.SetType(wildSlot);
                slot.PlayAnimation(7, true, animatedSlots.Count == 0);
                animatedSlots.Add(slot);

                // stagger new slot start
                yield return new WaitForSeconds(startDelay);

                // stop previous slot if exists
                if (animatedSlots.Count > 1)
                {
                    SuperBombSlotScript previousSlot =
                        animatedSlots[animatedSlots.Count - 2];

                    // delay stop without affecting stagger timing
                    StartCoroutine(StopAnimationDelayed(previousSlot, 7, stopDelay));
                    yield return new WaitForSeconds(0.4f);
                }
            }

            // stop the very last one after all
            if (animatedSlots.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(StopAnimationDelayed(
                    animatedSlots[animatedSlots.Count - 1], 7, stopDelay));
            }
        }
    }

    private IEnumerator StopAnimationDelayed(SuperBombSlotScript slot, int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        slot.StopAnimation(index);
    }

    private IEnumerator ShowPaylineWinsOnce()
    {
        if (activePaylines == null || activePaylines.Count == 0)
            yield break;

        yield return new WaitForSeconds(0.9f);

        foreach (var pl in activePaylines)
        {
            var matrix = pl.payline.ToMatrix();
            int totalReels = SuperBombSlotMachine.Instance.reels.Count;
            pl.payline.paylineImage.gameObject.SetActive(true);

            int start = pl.isLeftToRight ? 0 : totalReels - 1;
            int end = pl.isLeftToRight ? pl.reelLimit - 1 : totalReels - pl.reelLimit;
            int step = pl.isLeftToRight ? 1 : -1;

            List<SuperBombSlotScript> pathSlots = new List<SuperBombSlotScript>();

            for (int x = start; pl.isLeftToRight ? x <= end : x >= end; x += step)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (matrix[x, y] == 1)
                    {
                        var slot = SuperBombSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot != null)
                        {
                            pathSlots.Add(slot);
                            break; 
                        }
                    }
                }
            }

            if (pathSlots.Count > 0)
            {
                yield return StartCoroutine(
                    pathSlots[0].AnimatePaylineWinPath(
                        pl.paylineWinAmount,
                        pathSlots,
                        () =>
                        {
                            pl.payline.paylineImage.gameObject.SetActive(false);
                        }
                    )
                );
            }

            yield return new WaitForSeconds(0.01f);
        }

        yield return StartCoroutine(SuperBombSlotMachine.Instance.ShowTotalWins());
    }
    #endregion

    #region Slots Alpha Set and Reset

    private void SetSlotsAlpha(bool show)
    {
        paylineHighlighter.gameObject.SetActive(show);
    }
    #endregion
}
