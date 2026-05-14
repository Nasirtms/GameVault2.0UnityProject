using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SlotMachine2D;
using static UnityEngine.Rendering.DebugUI.Table;

public class RedHotTripplePaylineController : MonoBehaviour
{
    #region Variables

    public static RedHotTripplePaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<RedHotTripplePaylineData> paylines;

    private List<RedHotTripplePaylineEntry> activePaylines = new List<RedHotTripplePaylineEntry>();
    private readonly List<RedHotTrippleSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 2.5f;
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;

    private List<RedHotTripplePaylineResult> spinResults = new List<RedHotTripplePaylineResult>();

    private readonly Dictionary<int, int[]> paylinePatterns = new()
    {
        { 1, new[] { 1, 1, 1 } }, // Middle
        { 2, new[] { 0, 0, 0 } }, // Top
        { 3, new[] { 2, 2, 2 } }, // Bottom
        { 4, new[] { 0, 1, 2 } }, // Diagonal TL->BR
        { 5, new[] { 2, 1, 0 } }, // Diagonal BL->TR
    };
    public int resultSactterCount;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public API

    public void AddPaylineResult(RedHotTripplePaylineResult result)
    {
        if (!spinResults.Contains(result))
            spinResults.Add(result);
    }

    public void StartPaylineLoop(int scatterCount)
    {
        StopPaylineLoop();
        activePaylines.Clear();
        resultSactterCount = scatterCount;

        foreach (var result in spinResults)
        {
            var data = paylines.Find(p => p.paylineIndex == result.paylineNumber);
            if (data != null) activePaylines.Add(new RedHotTripplePaylineEntry(data, result.symbol, result.reelLimit));
        }

        if (activePaylines.Count == 0 && resultSactterCount < 3)
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
        if (scatterAnimation != null)
        {
            StopCoroutine(scatterAnimation);
            scatterAnimation = null;
        }
        StopAnimationsOnCurrentLine();
        HideAll();
    }

    #endregion

    #region Cycle

    private IEnumerator AnimateCycle()
    {
        isShowing = true;
        bool playOnlyOnce = RedHotTrippleSlotMachine.Instance.isFreeGame || RedHotTrippleAutoSpinController.isAutoSpinning;

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
            if (resultSactterCount >= 3)
            {
                scatterAnimation = StartCoroutine(ScatterAnimation());
                RedHotTrippleSlotMachine.Instance.isPaylineCompleted = true;
                break;
            }

            RedHotTrippleSlotMachine.Instance.isPaylineCompleted = true;
            if (playOnlyOnce)
                break;
        }
    }
    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < RedHotTrippleSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = RedHotTrippleSlotMachine.Instance.reels[x].slots[y + 2];
                if (slot == null) continue;

                if (slot.type == RedHotTrippleSlotType.BonusSymbol)
                {
                    slot.StartAnimation();
                    animatedSlots.Add(slot);
                }
            }
        }
        yield return new WaitForSeconds(2f);
        if (RedHotTrippleSlotMachine.Instance.freeSpinCount > 0 && !RedHotTrippleSlotMachine.Instance.isFreeGame)
        {
            RedHotTrippleSlotMachine.Instance.firstFreeSpin = true;
            RedHotTrippleUIManager.Instance.UpdateButtons("Transition Start");
            RedHotTrippleFreeGameTransitionController.Instance.StartFreeSpinTransition();
            RedHotTrippleFreeGameTransitionController.Instance.UpdateFreeSpinsCount(RedHotTrippleSlotMachine.Instance.freeSpinCount);
        }
        else if (RedHotTrippleSlotMachine.Instance.freeSpinCount > 0 && RedHotTrippleSlotMachine.Instance.isFreeGame)
        {
            RedHotTrippleFreeGameTransitionController.Instance.UpdateFreeSpinsCount(RedHotTrippleSlotMachine.Instance.freeSpinCount);
        }
        yield return new WaitForSeconds(1f);
    } 
    #endregion

    #region Visual Helpers

    private void Show(RedHotTripplePaylineData visual)
    {
        SetAlpha(visual.lineImage, 1f);
    }

    private void Hide(RedHotTripplePaylineData visual)
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

    #region Slot Animation 

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, RedHotTripplePaylineEntry entry)
    {
        StopAnimationsOnCurrentLine();

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }
        if (entry.reelLimit < 3)
        {
            for (int reel = 0; reel < RedHotTrippleSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = RedHotTrippleSlotMachine.Instance.reels[reel].slots[row + 2];
                if (slot == null) continue;

                if (slot.type == RedHotTrippleSlotType.WildSymbol)
                {
                    slot.StartAnimation();
                    animatedSlots.Add(slot);
                }
            }
        }
        else
        {
            for (int reel = 0; reel < RedHotTrippleSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = RedHotTrippleSlotMachine.Instance.reels[reel].slots[row + 2];
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

[System.Serializable]
public class RedHotTripplePaylineData
{
    public int paylineIndex;
    public Image lineImage;
}

public class RedHotTripplePaylineEntry
{
    public RedHotTripplePaylineData payline;
    public string symbol;
    public int reelLimit;
    public RedHotTripplePaylineEntry(RedHotTripplePaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public RedHotTripplePaylineData PaylineData => payline;
}

public class RedHotTripplePaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public RedHotTripplePaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}