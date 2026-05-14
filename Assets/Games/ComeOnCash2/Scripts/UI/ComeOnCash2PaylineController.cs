using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

[System.Serializable]
public class ComeOnCash2PaylineData
{
    public int paylineIndex;
}

[System.Serializable]
public class ComeOnCash2PaylineEntry
{
    private ComeOnCash2PaylineData payline;
    public ComeOnCash2PaylineEntry(ComeOnCash2PaylineData payline) { this.payline = payline; }
    public ComeOnCash2PaylineData PaylineData => payline;
}

[System.Serializable]
public class ComeOnCash2PaylineResult
{
    public int paylineNumber;
    public ComeOnCash2PaylineResult(int paylineNumber) { this.paylineNumber = paylineNumber; }
}

public class ComeOnCash2PaylineController : MonoBehaviour
{
    #region Variables

    public static ComeOnCash2PaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<ComeOnCash2PaylineData> paylines;

    //private List<CashMachinePaylineEntry> activePaylines = new();
    //private readonly List<CashMachineSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.5f;
    //private Coroutine animationLoop;
    //private bool isShowing = false;

    private List<ComeOnCash2PaylineResult> spinResults = new();

    // 🔧 EDIT THIS to match your actual 3‑reel paylines:
    // Each array is 3 ints = row index per reel (0=top, 1=middle, 2=bottom)
    private readonly Dictionary<int, int[]> paylinePatterns = new()
    {
        { 1, new[] { 1, 1, 1 } }, // Middle
        { 2, new[] { 0, 0, 0 } }, // Top
        { 3, new[] { 2, 2, 2 } }, // Bottom
        { 4, new[] { 0, 1, 2 } }, // Diagonal TL->BR
        { 5, new[] { 2, 1, 0 } }, // Diagonal BL->TR
    };

    #endregion

    #region Unity

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public API

    //public void AddPaylineResult(CashMachinePaylineResult result)
    //{
    //    if (!spinResults.Contains(result))
    //        spinResults.Add(result);
    //}

    //public void StartPaylineLoop()
    //{
    //    StopPaylineLoop();
    //    activePaylines.Clear();

    //    foreach (var result in spinResults)
    //    {
    //        var data = paylines.Find(p => p.paylineIndex == result.paylineNumber);
    //        if (data != null) activePaylines.Add(new CashMachinePaylineEntry(data));
    //    }

    //    if (activePaylines.Count == 0)
    //    {
    //        Debug.LogWarning("No paylines to display.");
    //        return;
    //    }

    //    isShowing = true;
    //    animationLoop = StartCoroutine(AnimateCycle());
    //}

    public void ClearPaylineResults() => spinResults.Clear();

    public void StopPaylineLoop()
    {
        StopPaylineSlots();
        //isShowing = false;

        //if (animationLoop != null)
        //{
        //    StopCoroutine(animationLoop);
        //    animationLoop = null;
        //}

        //StopWildAnimationsOnCurrentLine();
    }

    #endregion

    #region Cycle

    //private IEnumerator AnimateCycle()
    //{
    //    isShowing = true;

    //    while (isShowing)
    //    {
    //        foreach (var entry in activePaylines)
    //        {
    //            var visual = entry.PaylineData;
    //            if (visual == null) continue;

    //            PlayWildAnimationsForPayline(visual.paylineIndex);

    //            yield return new WaitForSeconds(holdDuration);

    //            StopWildAnimationsOnCurrentLine();

    //            yield return new WaitForSeconds(fadeDuration);
    //        }
    //        CashMachineSlotMachine.Instance.isPaylineCompleted = true;
    //        if (CashMachineSlotMachine.Instance.isFreeGameReady)
    //        {
    //            CashMachineSlotMachine.Instance.isFreeGameReady = false;
    //            yield return PlayBullseyeBorderThenTransition();
    //        }
    //    }
    //}

    #endregion


    #region Slot animation (trigger bullseye only)

    //private void PlayWildAnimationsForPayline(int paylineIndex)
    //{
    //    StopWildAnimationsOnCurrentLine(); // safety

    //    if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
    //    {
    //        // No pattern for this payline; nothing to animate.
    //        return;
    //    }

    //    // Expecting 3 reels; if you change reel count, update logic below.
    //    for (int reel = 0; reel < CashMachineSlotMachine.Instance.reels.Count; reel++)
    //    {
    //        int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
    //        var slot = CashMachineSlotMachine.Instance.reels[reel].slots[row + 1]; // [1..3] are visible
    //        if (slot == null) continue;

    //        //if (slot.type == DoubleJackpotBullseyeSlotType.DoubleJackpotBullseye)
    //        //{
    //            slot.StartAnimation();
    //            animatedSlots.Add(slot);
    //        //}
    //    }
    //}

    //private void StopWildAnimationsOnCurrentLine()
    //{
    //    if (animatedSlots.Count == 0) return;
    //    foreach (var s in animatedSlots)
    //    {
    //        if (s != null) s.StopAnimation();
    //    }
    //    animatedSlots.Clear();
    //}

    //private IEnumerator PlayBullseyeBorderThenTransition()
    //{
    //    var sm = CashMachineSlotMachine.Instance;
    //    if (sm.reels.Count < 3) yield break;

    //    var middle = sm.reels[1];
    //    CashMachineSlotScript bullseyeSlot = null;

    //    for (int row = 0; row < 3; row++)
    //    {
    //        var s = middle.slots[row + 1]; // visible are 1..3
    //        //if (s != null && s.type == CashMachineSlotType.DoubleJackpotBullseye)
    //        //{
    //        //    bullseyeSlot = s; break;
    //        //}
    //    }

    //    // Pulse the Bullseye border briefly (if found)
    //    if (bullseyeSlot != null)
    //    {
    //        bullseyeSlot.StartAnimation();
    //        yield return new WaitForSeconds(0.8f);
    //        bullseyeSlot.StopAnimation();
    //    }
    //    else
    //    {
    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    // Start the free-spin transition now
    //    CashMachineFreeGameTransitionController.Instance.StartFreeSpinTransition();
    //}
    #endregion

    public void PlayPaylineSlots()
    {
        for(int reel = 0; reel < ComeOnCash2SlotMachine.Instance.reels.Count; reel++)
        {
            var slot = ComeOnCash2SlotMachine.Instance.reels[reel].slots[1];
            if (slot == null) continue;

            if(!ComeOnCash2SlotMachine.Instance.canReel1spin && reel == 0)
            {
                continue;
            }

            if(!ComeOnCash2SlotMachine.Instance.canReel2spin && reel == 1)
            {
                continue;
            }

            if(!ComeOnCash2SlotMachine.Instance.canReel3spin && reel == 2)
            {
                continue;
            }

            slot.AnimateSlots();
        }
        ComeOnCash2SlotMachine.Instance.isPaylineCompleted = true;
    }

    private void StopPaylineSlots()
    {
        for (int reel = 0; reel < ComeOnCash2SlotMachine.Instance.reels.Count; reel++)
        {
            var slot = ComeOnCash2SlotMachine.Instance.reels[reel].slots[1];
            if (slot == null) continue;
            slot.DeAnimateSlots();
        }

        ComeOnCash2UIManager.Instance.reel1PaylineBg.SetActive(false);
        if(ComeOnCash2UIManager.Instance.reel2PaylineBg.activeSelf) ComeOnCash2UIManager.Instance.reel2PaylineBg.SetActive(false);
        if(ComeOnCash2UIManager.Instance.reel3PaylineBg.activeSelf) ComeOnCash2UIManager.Instance.reel3PaylineBg.SetActive(false);
    }
}
