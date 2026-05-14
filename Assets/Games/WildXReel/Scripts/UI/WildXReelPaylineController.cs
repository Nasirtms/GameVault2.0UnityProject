using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WildXReelPaylineData
{
    public int paylineIndex;
}

public class WildXReelPaylineEntry
{
    public WildXReelPaylineData payline;
    public string symbol;
    public int reelLimit;
    public WildXReelPaylineEntry(WildXReelPaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public WildXReelPaylineData PaylineData => payline;
}

public class WildXReelPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public WildXReelPaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}
public class WildXReelPaylineController : MonoBehaviour
{
    #region Variables

    public static WildXReelPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<WildXReelPaylineData> paylines;

    private List<WildXReelPaylineEntry> activePaylines = new List<WildXReelPaylineEntry>();
    private readonly List<WildXReelSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.3f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<WildXReelPaylineResult> spinResults = new List<WildXReelPaylineResult>();

    private readonly Dictionary<int, int[]> paylinePatterns = new()
    {
        { 1, new[] { 1, 1, 1 } },
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

    public void AddPaylineResult(WildXReelPaylineResult result)
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
            if (data != null) activePaylines.Add(new WildXReelPaylineEntry(data, result.symbol, result.reelLimit));
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
            WildXReelSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion


    #region Slot animation 

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, WildXReelPaylineEntry entry)
    {
        StopAnimationsOnCurrentLine();

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
            return;

        for (int reel = 0; reel < WildXReelSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = WildXReelSlotMachine.Instance.reels[reel].slots[row + 2];
            if (slot == null) continue;

            slot.StartAnimation();
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