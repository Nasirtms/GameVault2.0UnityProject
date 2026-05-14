using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ImperialDiamondPaylineData
{
    public int paylineIndex;
    public Image lineImage;
}

public class ImperialDiamondPaylineEntry
{
    public ImperialDiamondPaylineData payline;
    public string symbol;
    public int reelLimit;
    public ImperialDiamondPaylineEntry(ImperialDiamondPaylineData payline, string symbol, int reelLimit)
    {
        this.payline = payline;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
    public ImperialDiamondPaylineData PaylineData => payline;
}

public class ImperialDiamondPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string symbol;
    public ImperialDiamondPaylineResult(int paylineNumber, string symbol, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.symbol = symbol;
        this.reelLimit = reelLimit;
    }
}
public class ImperialDiamondPaylineController : MonoBehaviour
{
    #region Variables

    public static ImperialDiamondPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<ImperialDiamondPaylineData> paylines;

    private List<ImperialDiamondPaylineEntry> activePaylines = new List<ImperialDiamondPaylineEntry>();
    private readonly List<ImperialDiamondSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.5f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<ImperialDiamondPaylineResult> spinResults = new List<ImperialDiamondPaylineResult>();

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

    public void AddPaylineResult(ImperialDiamondPaylineResult result)
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
            if (data != null) activePaylines.Add(new ImperialDiamondPaylineEntry(data, result.symbol, result.reelLimit));
        }

        if (activePaylines.Count == 0)
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
                //Hide(visual);

                yield return new WaitForSeconds(fadeDuration);
            }
            ImperialDiamondSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion

    #region Visual helpers

    public void Show(ImperialDiamondPaylineData visual)
    {
        SetAlpha(visual.lineImage, 1f);
    }

    public void Hide(ImperialDiamondPaylineData visual)
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

    private void PlayAnimationsForPayline(string symbol, int paylineIndex, ImperialDiamondPaylineEntry entry)
    {
        StopAnimationsOnCurrentLine();

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            return;
        }
        if (entry.reelLimit < 3)
        {
            for (int reel = 0; reel < ImperialDiamondSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = ImperialDiamondSlotMachine.Instance.reels[reel].slots[row + 2];
                if (slot == null) continue;

                if (slot.type == ImperialDiamondSlotType.ImperialDiamond)
                {
                    slot.StartAnimation();
                    animatedSlots.Add(slot);
                }
            }
        }
        else
        {
            for (int reel = 0; reel < ImperialDiamondSlotMachine.Instance.reels.Count; reel++)
            {
                int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
                var slot = ImperialDiamondSlotMachine.Instance.reels[reel].slots[row + 2];
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