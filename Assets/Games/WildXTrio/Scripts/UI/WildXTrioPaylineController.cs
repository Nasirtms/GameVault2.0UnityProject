using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WildXTrioPaylineController : MonoBehaviour
{
    #region Variables

    public static WildXTrioPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<WildXTrioPaylineData> paylines;

    private List<WildXTrioPaylineEntry> activePaylines = new List<WildXTrioPaylineEntry>();
    private readonly List<WildXTrioSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 2f;
    private Coroutine animationLoop;
    private bool isShowing = false;
    public Image lineImage;
    private List<WildXTrioPaylineResult> spinResults = new List<WildXTrioPaylineResult>();

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

    public void AddPaylineResult(WildXTrioPaylineResult result)
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
            if (data != null) activePaylines.Add(new WildXTrioPaylineEntry(data, result.symbol, result.reelLimit));
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
                lineImage.gameObject.SetActive(true);
                PlayAnimationsForPayline(entry.symbol, visual.paylineIndex, entry);

                //yield return new WaitForSeconds(holdDuration);

                //StopAnimationsOnCurrentLine();
                //Hide(visual);

                yield return new WaitForSeconds(fadeDuration);
            }
            WildXTrioSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion

    #region Slot animation 

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, WildXTrioPaylineEntry entry)
    {
        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }
        
        for (int reel = 0; reel < WildXTrioSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = WildXTrioSlotMachine.Instance.reels[reel].slots[row + 2];
            if (slot == null) continue;

            slot.PlayAnimation();
            animatedSlots.Add(slot);
        }
    }

    private void StopAnimationsOnCurrentLine()
    {
        if (animatedSlots.Count == 0) return;
        lineImage.gameObject.SetActive(false);  
        foreach (var s in animatedSlots)
        {
            if (s != null) s.StopAnimation();
        }
        animatedSlots.Clear();
    }

    #endregion
}

[System.Serializable]
public class WildXTrioPaylineData
{
    public int paylineIndex;
}

public class WildXTrioPaylineEntry
{
    public WildXTrioPaylineData payline;
    public string symbol;
    public int reelLimit;
    public WildXTrioPaylineEntry(WildXTrioPaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public WildXTrioPaylineData PaylineData => payline;
}

public class WildXTrioPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public WildXTrioPaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}