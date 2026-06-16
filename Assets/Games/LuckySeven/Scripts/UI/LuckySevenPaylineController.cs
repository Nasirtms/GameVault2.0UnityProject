using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LuckySevenPaylineController : MonoBehaviour
{
    #region Variables

    public static LuckySevenPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<LuckySevenPaylineData> paylines;

    private List<LuckySevenPaylineEntry> activePaylines = new List<LuckySevenPaylineEntry>();
    private readonly List<LuckySevenSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 2f;
    private Coroutine animationLoop;
    private bool isShowing = false;
    private List<LuckySevenPaylineResult> spinResults = new List<LuckySevenPaylineResult>();

    private readonly Dictionary<int, int[]> paylinePatterns = new()
    {
        { 1, new[] { 0, 0, 0 } },
        { 2, new[] { 1, 1, 1 } },
        { 3, new[] { 2, 2, 2 } },
        { 4, new[] { 0, 1, 2 } },
        { 5, new[] { 2, 1, 0 } },
        { 6, new[] { 0, 1, 0 } },
        { 7, new[] { 0, 2, 0 } },
        { 8, new[] { 1, 0, 1 } },
        { 9, new[] { 1, 2, 1 } },
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

    public void AddPaylineResult(LuckySevenPaylineResult result)
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
            if (data != null) activePaylines.Add(new LuckySevenPaylineEntry(data, result.symbol, result.reelLimit));
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
            LuckySevenSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion

    #region Slot animation 

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, LuckySevenPaylineEntry entry)
    {
        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }
        
        for (int reel = 0; reel < LuckySevenSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = LuckySevenSlotMachine.Instance.reels[reel].slots[row + 2];
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
public class LuckySevenPaylineData
{
    public int paylineIndex;

    public List<int> pattern = new List<int>(new int[9]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[3, 3]; // [columns, rows]
        for (int x = 0; x < 3; x++)     // columns
        {
            for (int y = 0; y < 3; y++) // rows
            {
                matrix[x, y] = pattern[y * 3 + x];
            }
        }
        return matrix;
    }
}

public class LuckySevenPaylineEntry
{
    public LuckySevenPaylineData payline;
    public string symbol;
    public int reelLimit;
    public LuckySevenPaylineEntry(LuckySevenPaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public LuckySevenPaylineData PaylineData => payline;
}

public class LuckySevenPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public LuckySevenPaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}