using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.CompilerServices;

[System.Serializable]
public class VegasSevenPaylineData
{
    public int paylineIndex;
}

public class VegasSevenPaylineEntry
{
    private VegasSevenPaylineData payline;
    public string symbol;
    public VegasSevenPaylineEntry(VegasSevenPaylineData payline, string symbol) { this.payline = payline;this.symbol = symbol; }
    public VegasSevenPaylineData PaylineData => payline;
}

public class VegasSevenPaylineResult
{
    public int paylineNumber;
    public string symbol;
    public VegasSevenPaylineResult(int paylineNumber,string symbol) { this.paylineNumber = paylineNumber; this.symbol = symbol; }
}

public class VegasSevenPaylineController : MonoBehaviour
{
    #region Variables

    public static VegasSevenPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<VegasSevenPaylineData> paylines;

    private List<VegasSevenPaylineEntry> activePaylines = new();
    [SerializeField] public List<VegasSevenSlotScript> animatedSlots = new();

    private int resultScatterCount;
    private bool freeGameTriggered;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.9f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<VegasSevenPaylineResult> spinResults = new();

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

    public void AddPaylineResult(VegasSevenPaylineResult result)
    {
        if (!spinResults.Contains(result))
            spinResults.Add(result);
    }

    public void StartPaylineLoop(int scatterCount)
    {
        StopPaylineLoop();
        activePaylines.Clear();

        resultScatterCount = scatterCount;
        freeGameTriggered = false;

        foreach (var result in spinResults)
        {
            var data = paylines.Find(p => p.paylineIndex == result.paylineNumber);
            if (data != null) activePaylines.Add(new VegasSevenPaylineEntry(data, result.symbol));
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3)
        {
            Debug.LogWarning("No paylines to display.");
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(AnimateCycle());
    }

    public void ClearPaylineResults() => spinResults.Clear();

    public void StopPaylineLoop()
    {
        isShowing = false;
        resultScatterCount = 0;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        StopWildAnimationsOnCurrentLine();
    }

    #endregion

    #region Cycle

    private IEnumerator AnimateCycle()
    {
        isShowing = true;

        while (isShowing)
        {
            foreach (var entry in activePaylines)
            {
                var visual = entry.PaylineData;
                if (visual == null) continue;

                PlayWildAnimationsForPayline(entry.symbol, visual.paylineIndex);

                yield return new WaitForSeconds(holdDuration);

                StopWildAnimationsOnCurrentLine();

                yield return new WaitForSeconds(fadeDuration);
            }

            if (resultScatterCount >= 3 && !VegasSevenSlotMachine.Instance.isFreeGame)
            {
                VegasSevenUIManager.Instance.PlaySound("FreeSpinStart");

                VegasSevenSlotMachine.Instance.firstFreeSpin = true;
                VegasSevenSlotMachine.Instance.isPaylineCompleted = true;

                VegasSevenFreeGameTransitionController.Instance.StartFreeSpinTransition();
                VegasSevenFreeGameTransitionController.Instance.UpdateFreeSpinsCount(VegasSevenSlotMachine.Instance.freeSpinCount);
                yield break;
            }
            else if (resultScatterCount >= 3)
            {
                VegasSevenSlotMachine.Instance.isPaylineCompleted = true;
                //VegasSevenSlotMachine.Instance.IncreaseRetrigger();
                VegasSevenFreeGameTransitionController.Instance.UpdateFreeSpinsCount(VegasSevenSlotMachine.Instance.freeSpinCount);
                yield break;
            }

            VegasSevenSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion

 
    #region Slot animation (trigger wilds only)
    private void PlayWildAnimationsForPayline(string symbol,int paylineIndex)
    {
        //StopWildAnimationsOnCurrentLine(); // safety

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            // No pattern for this payline; nothing to animate.
            return;
        }
        if (symbol.Contains("RedHot3X"))
        {
            for (int reel = 0; reel < VegasSevenSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = VegasSevenSlotMachine.Instance.reels[reel].slots[row + 2]; // [1..3] are visible
                if (slot == null) continue;

                if (slot.type == VegasSevenSlotType.RedHot3X)
                {
                    slot.StartAnimation();
                    animatedSlots.Add(slot);
                }
            }

        }
        else
        {
            // Expecting 3 reels; if you change reel count, update logic below.
            for (int reel = 0; reel < VegasSevenSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = VegasSevenSlotMachine.Instance.reels[reel].slots[row + 2]; // [1..3] are visible
                if (slot == null) continue;

                slot.StartAnimation();
                animatedSlots.Add(slot);
            }
        }
    }

    public void StopWildAnimationsOnCurrentLine()
    {
        if (animatedSlots.Count == 0) return;
        foreach (var s in animatedSlots)
        {
            if (s != null) s.StopAnimation();
        }
        animatedSlots.Clear();
    }

    #endregion
}
