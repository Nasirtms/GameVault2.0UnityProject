using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WolfMoonLinkPaylineController : MonoBehaviour
{
    #region Variables

    public static WolfMoonLinkPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<WolfMoonLinkPaylineData> paylines;

    private List<WolfMoonLinkPaylineEntry> activePaylines = new List<WolfMoonLinkPaylineEntry>();
    private readonly List<WolfMoonLinkSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 2f;

    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<WolfMoonLinkPaylineResult> spinResults = new List<WolfMoonLinkPaylineResult>();

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

    public void AddPaylineResult(WolfMoonLinkPaylineResult result)
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

            if (data != null)
                activePaylines.Add(new WolfMoonLinkPaylineEntry(data, result.symbol, result.reelLimit));
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
        //HideAll();
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

                //Show(visual);
                PlayAnimationsForPayline(entry.symbol, visual.paylineIndex, entry);

                yield return new WaitForSeconds(holdDuration);

                StopAnimationsOnCurrentLine();
                //Hide(visual);

                yield return new WaitForSeconds(fadeDuration);
            }

            WolfMoonLinkSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion

    #region Visual helpers

    //private void Show(WolfMoonLinkPaylineData visual)
    //{
    //    SetAlpha(visual.lineImage, 1f);
    //}

    //private void Hide(WolfMoonLinkPaylineData visual)
    //{
    //    SetAlpha(visual.lineImage, 0f);
    //}

    //private void HideAll()
    //{
    //    foreach (var visual in paylines) Hide(visual);
    //}

    //private void SetAlpha(Image img, float alpha)
    //{
    //    if (!img) return;
    //    var c = img.color; c.a = alpha; img.color = c;
    //}

    #endregion
    #region Slot animation

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, WolfMoonLinkPaylineEntry entry)
    {
        StopAnimationsOnCurrentLine();
        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }

        for (int reel = 0; reel < WolfMoonLinkSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = WolfMoonLinkSlotMachine.Instance.reels[reel].slots[row + 2];

            if (slot == null) continue;

            slot.PlayAnimation();
            animatedSlots.Add(slot);
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
public class WolfMoonLinkPaylineData
{
    public int paylineIndex;
    //public Image lineImage;
}

public class WolfMoonLinkPaylineEntry
{
    public WolfMoonLinkPaylineData payline;
    public string symbol;
    public int reelLimit;

    public WolfMoonLinkPaylineEntry(WolfMoonLinkPaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }

    public WolfMoonLinkPaylineData PaylineData => payline;
}

public class WolfMoonLinkPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;

    public WolfMoonLinkPaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}