using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.CompilerServices;

[System.Serializable]
public class AtomicMeltdownPaylineData
{
    public int paylineIndex;
    public Image lineImage;
    public Image leftIcon;
    public Image rightIcon;
}

public class AtomicMeltdownPaylineEntry
{
    private AtomicMeltdownPaylineData payline;
    public AtomicMeltdownPaylineEntry(AtomicMeltdownPaylineData payline) { this.payline = payline; }
    public AtomicMeltdownPaylineData PaylineData => payline;
}

public class AtomicMeltdownPaylineResult
{
    public int paylineNumber;
    public AtomicMeltdownPaylineResult(int paylineNumber) { this.paylineNumber = paylineNumber; }
}

public class AtomicMeltdownPaylineController : MonoBehaviour
{
    #region Variables

    public static AtomicMeltdownPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<AtomicMeltdownPaylineData> paylines;

    private List<AtomicMeltdownPaylineEntry> activePaylines = new();
    private readonly List<AtomicMeltdownSlotScript> animatedSlots = new();

    private int resultFreeSpin;
    private bool freeGameTriggered;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.5f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<AtomicMeltdownPaylineResult> spinResults = new();

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

    public void AddPaylineResult(AtomicMeltdownPaylineResult result)
    {
        if (!spinResults.Contains(result))
            spinResults.Add(result);
    }

    public void StartPaylineLoop(int freeSpinCount)
    {
        StopPaylineLoop();
        activePaylines.Clear();

        resultFreeSpin = freeSpinCount;
        freeGameTriggered = false;

        foreach (var result in spinResults)
        {
            var data = paylines.Find(p => p.paylineIndex == result.paylineNumber);
            if (data != null) activePaylines.Add(new AtomicMeltdownPaylineEntry(data));
        }

        if (activePaylines.Count == 0 && resultFreeSpin <= 0)
        {
            //Debug.LogWarning("No paylines to display.");
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(AnimateCycle());
    }

    public void ClearPaylineResults() => spinResults.Clear();

    public void StopPaylineLoop()
    {
        isShowing = false;
        resultFreeSpin = 0;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        StopWildAnimationsOnCurrentLine();
        HideAll();
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

                Show(visual);
                PlayWildAnimationsForPayline(visual.paylineIndex);

                yield return new WaitForSeconds(holdDuration);

                StopWildAnimationsOnCurrentLine();
                Hide(visual);

                yield return new WaitForSeconds(fadeDuration);
            }

            if (resultFreeSpin > 0)
            {
                AtomicMeltdownSlotMachine.Instance.firstFreeSpin = true;
                AtomicMeltdownSlotMachine.Instance.isPaylineCompleted = true;

                AtomicMeltdownFreeGameTransitionController.Instance.StartFreeSpinTransition();
                AtomicMeltdownFreeGameTransitionController.Instance.UpdateFreeSpinsCount(resultFreeSpin);

                break;
            }

            AtomicMeltdownSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion

    #region Visual helpers

    private void Show(AtomicMeltdownPaylineData visual)
    {
        SetAlpha(visual.lineImage, 1f);
        SetAlpha(visual.leftIcon, 1f);
        SetAlpha(visual.rightIcon, 1f);
    }

    private void Hide(AtomicMeltdownPaylineData visual)
    {
        SetAlpha(visual.lineImage, 0.3f);
        SetAlpha(visual.leftIcon, 0f);
        SetAlpha(visual.rightIcon, 0f);
    }

    private void HideAll()
    {
        foreach (var visual in paylines) Hide(visual);
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (!img) return;
        var c = img.color; c.a = alpha; img.color = c;
    }

    #endregion

    #region Slot animation (trigger wilds only)

    private static bool IsWild(AtomicMeltdownSlotType t)
    {
        return t == AtomicMeltdownSlotType.Wild10x
            || t == AtomicMeltdownSlotType.Wild5x
            || t == AtomicMeltdownSlotType.Wild3x
            || t == AtomicMeltdownSlotType.Wild2x;
    }

    private void PlayWildAnimationsForPayline(int paylineIndex)
    {
        StopWildAnimationsOnCurrentLine(); // safety

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            // No pattern for this payline; nothing to animate.
            return;
        }

        // Expecting 3 reels; if you change reel count, update logic below.
        for (int reel = 0; reel < AtomicMeltdownSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = AtomicMeltdownSlotMachine.Instance.reels[reel].slots[row + 1]; // [1..3] are visible
            if (slot == null) continue;

            //if (IsWild(slot.type))
            //{
            //    slot.StartAnimation();
            //    animatedSlots.Add(slot);
            //}

            slot.StartAnimation();
            animatedSlots.Add(slot);
        }
    }

    private void StopWildAnimationsOnCurrentLine()
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
