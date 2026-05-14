using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WheelOfFortunePaylineData
{
    public int paylineIndex;
    public Image lineImage;
}

public class WheelOfFortunePaylineEntry
{
    public WheelOfFortunePaylineData payline;
    public string symbol;
    public int reelLimit;
    public WheelOfFortunePaylineEntry(WheelOfFortunePaylineData payline, string symbol, int reelLimit) 
    { 
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public WheelOfFortunePaylineData PaylineData => payline;
}

public class WheelOfFortunePaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public WheelOfFortunePaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}
public class WheelOfFortunePaylineController : MonoBehaviour
{
    #region Variables

    public static WheelOfFortunePaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<WheelOfFortunePaylineData> paylines;

    private List<WheelOfFortunePaylineEntry> activePaylines = new List<WheelOfFortunePaylineEntry>();
    private readonly List<WheelOfFortuneSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.5f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<WheelOfFortunePaylineResult> spinResults = new List<WheelOfFortunePaylineResult>();

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

    public void AddPaylineResult(WheelOfFortunePaylineResult result)
    {
        if (!spinResults.Contains(result))
            spinResults.Add(result);
    }

    public void StartPaylineLoop()
    {
        StopPaylineLoop();
        activePaylines.Clear();

        foreach (var result in spinResults)
        {
            var data = paylines.Find(p => p.paylineIndex == result.paylineNumber);
            if (data != null) activePaylines.Add(new WheelOfFortunePaylineEntry(data, result.symbol, result.reelLimit));
        }

        if (activePaylines.Count == 0)
        {
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(AnimateCycle());
    }

    public void ClearPaylineResults() => spinResults.Clear();

    public void StopPaylineLoop()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        StopAnimationsOnCurrentLine();
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
                PlayAnimationsForPayline(entry.symbol, visual.paylineIndex, entry);

                yield return new WaitForSeconds(holdDuration);

                StopAnimationsOnCurrentLine();
                Hide(visual);

                yield return new WaitForSeconds(fadeDuration);
            }

            WheelOfFortuneSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion

    #region Visual helpers

    private void Show(WheelOfFortunePaylineData visual)
    {
        SetAlpha(visual.lineImage, 1f);
    }

    private void Hide(WheelOfFortunePaylineData visual)
    {
        SetAlpha(visual.lineImage, 0f);
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

    #region Slot animation 

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, WheelOfFortunePaylineEntry entry)
    {
        StopAnimationsOnCurrentLine();

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }
        if (entry.reelLimit < 3)
        {
            for (int reel = 0; reel < WheelOfFortuneSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = WheelOfFortuneSlotMachine.Instance.reels[reel].slots[row + 2];
                if (slot == null) continue;

                if (slot.type == WheelOfFortuneSlotType.Cherry)
                {
                    slot.StartAnimation();
                    animatedSlots.Add(slot);
                }
            }
        }
        else
        {
            for (int reel = 0; reel < WheelOfFortuneSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = WheelOfFortuneSlotMachine.Instance.reels[reel].slots[row + 2]; 
                if (slot == null) continue;

                slot.StartAnimation();
                animatedSlots.Add(slot);
            }
        }
    }

    private void StopAnimationsOnCurrentLine()
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