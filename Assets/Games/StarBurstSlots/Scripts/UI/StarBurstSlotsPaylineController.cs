using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static SlotMachine2D;
using static UnityEngine.EventSystems.EventTrigger;

public class StarBurstSlotsPaylineController : MonoBehaviour
{
    #region Variables

    public static StarBurstSlotsPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<StarBurstSlotsPaylineData> paylines;

    // Currently running paylines
    public List<StarBurstSlotsPaylineEntry> activePaylines = new List<StarBurstSlotsPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;
    [SerializeField] private GameObject paylineHighlighter;
    public bool dontShowComboVfxInSpin = false;
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true
    private bool showPaylineWinOnce = false;
    //public Coroutine paylineRoutine;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<StarBurstSlotsPaylineResult> spinResult = new List<StarBurstSlotsPaylineResult>();
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void AddPaylineData(StarBurstSlotsPaylineResult result)
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
            for (int x = 0; x < StarBurstSlotsSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
                    int idx = slot.currentResource.index;
                    var slotimage = slot.slots[idx];
                    SetSlotsAlpha(false);
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        entry.payline.paylineImage.gameObject.SetActive(false);
                        slot.StopAnimation(idx);
                        slot.HidePaylineWin();
                    }
                }
            }
        }
        for (int x = 0; x < StarBurstSlotsSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
                var idx = slot.currentResource.index;
                if (slot != null && slot.type == StarBurstSlotsSlotType.Wild)
                {
                    slot.StopAnimation(idx);
                    slot.HidePaylineWin();
                }
            }
        }
        spinResult.Clear();
        activePaylines.Clear();
    }
    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<StarBurstSlotsPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new StarBurstSlotsPaylineEntry(
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
            StarBurstSlotsSlotMachine.Instance.isPaylineCompleted = true;
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
            StarBurstSlotsSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }
        if (!StarBurstSlotsAutoSpinController.isAutoSpinning && !StarBurstSlotsSlotMachine.Instance.isFreeGame)
        {
            StarBurstSlotsSlotMachine.Instance.isPaylineCompleted = true;
        }
        if (activePaylines.Count == 1)
        {
            yield return StartCoroutine(ShowPaylineWinsOnce());
            //paylineRoutine = StartCoroutine(PlayAllPaylines());
            yield return PlayAllPaylines();
        }
        else
        {
            if (StarBurstSlotsAutoSpinController.isAutoSpinning)
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

        StarBurstSlotsSlotMachine.Instance.isPaylineCompleted = true;
    }
    public IEnumerator PlayAllPaylines()
    {
        int index = 0;
        int maxIndex = activePaylines.Count;
        if (!dontShowComboVfxInSpin)
        {
            StarBurstSlotsUIManager.Instance.ShowComboVfx(maxIndex);
            StarBurstSlotsUIManager.Instance.hasShowenComboVfx = true;
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
                int totalReels_ = StarBurstSlotsSlotMachine.Instance.reels.Count;

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
                            if (y + 1 >= StarBurstSlotsSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
                            if (slot == null) continue;

                            if (pl.payline.ToMatrix()[x, y] == 1)
                            {
                                int idx = slot.currentResource.index;
                                var slotimage = slot.slots[idx];
                                pl.payline.paylineImage.gameObject.SetActive(false);    
                                slot.StopAnimation(idx);
                                SetSlotsAlpha(false);
                                slot.HidePaylineWin();
                            }
                        }
                    }
                }
            }

            // safe wrap
            if (activePaylines.Count == 0) yield break;
            index = index % activePaylines.Count;

            var currentPayline = activePaylines[index];

            int totalReels = StarBurstSlotsSlotMachine.Instance.reels.Count;

            // Figure out start, end, and direction step
            int start = currentPayline.isLeftToRight ? 0 : totalReels - 1;
            int end = currentPayline.isLeftToRight ? currentPayline.reelLimit : totalReels - currentPayline.reelLimit;
            int step = currentPayline.isLeftToRight ? 1 : -1;

            for (int x = start; currentPayline.isLeftToRight ? x < end : x >= end; x += step)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot_ = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot_ == null) continue;

                    int idx = slot_.currentResource.index;
                    var slotimage_ = slot_.slots[idx];

                    if (currentPayline.payline.ToMatrix()[x, y] == 1)
                    {
                        if (y + 1 >= StarBurstSlotsSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
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
                            slot.ShowPaylineWin(currentPayline.paylineWinAmount);
                        }
                    }

                }
            }

            yield return new WaitForSeconds(1.8f);



            if (StarBurstSlotsAutoSpinController.isAutoSpinning || StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
            {
                if (index + 1 == maxIndex)
                {
                    StarBurstSlotsSlotMachine.Instance.isPaylineCompleted = true;
                    if (StarBurstSlotsSlotMachine.Instance.isFreeGameReady && StarBurstSlotsSlotMachine.Instance.freeSpinCount > 0)
                    {
                        ClearPaylineData();
                        StarBurstSlotsSlotMachine.Instance.SetSlotAnimationCompleted();
                    }
                    yield break;
                }
            }
            index++;
        }
    }

    public IEnumerator ConvertTriggeredReelsToWild()
    {
        if (StarBurstSlotsSlotMachine.Instance == null)
            yield break;

        var slotMachine = StarBurstSlotsSlotMachine.Instance;

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

            List<StarBurstSlotsSlotScript> animatedSlots = new List<StarBurstSlotsSlotScript>();

            foreach (var slot in reel.slots)
            {
                if (slot == null) continue;

                var wildSlot = slotMachine.settings.resourcesList
                    .Find(r => r.type == StarBurstSlotsSlotType.Wild);

                if(animatedSlots.Count == 0)
                {
                    StarBurstSlotsUIManager.Instance.PlaySound("wild");
                }

                slot.SetType(wildSlot);
                slot.PlayAnimation(7, true, animatedSlots.Count == 0);
                animatedSlots.Add(slot);

                // stagger new slot start
                yield return new WaitForSeconds(startDelay);

                // stop previous slot if exists
                if (animatedSlots.Count > 1)
                {
                    StarBurstSlotsSlotScript previousSlot =
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

    private IEnumerator StopAnimationDelayed(
        StarBurstSlotsSlotScript slot, int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        slot.StopAnimation(index);
    }

    private IEnumerator ShowPaylineWinsOnce()
    {
        if (activePaylines == null || activePaylines.Count == 0)
            yield break;

        List<(StarBurstSlotsSlotScript slot, float winAmount, int index)> slotsToShow = new List<(StarBurstSlotsSlotScript, float, int)>();

        foreach (var pl in activePaylines)
        {
            var matrix = pl.payline.ToMatrix();
            int totalReels_ = StarBurstSlotsSlotMachine.Instance.reels.Count;

            // Figure out start, end, and direction step
            int start_ = pl.isLeftToRight ? 0 : totalReels_ - 1;
            int end_ = pl.isLeftToRight ? pl.reelLimit : totalReels_ - pl.reelLimit;
            int step_ = pl.isLeftToRight ? 1 : -1;

            for (int x = start_; pl.isLeftToRight ? x < end_ : x >= end_; x += step_)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (matrix[x, y] == 1)
                    {
                        if (y + 1 >= StarBurstSlotsSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = StarBurstSlotsSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (!slotsToShow.Any(s => s.slot == slot))
                            slotsToShow.Add((slot, pl.paylineWinAmount, slot.currentResource.index));
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.9f);
        foreach (var s in slotsToShow)
        {
            if (s.index > 4)
            {
                continue;
            }
            else
            {
                s.slot.showPaylineWinOnce(s.winAmount, s.index);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    #endregion

    #region Slots Alpha Set and Reset

    private void SetSlotsAlpha(bool show)
    {
        paylineHighlighter.gameObject.SetActive(show);
    }
    #endregion
}
