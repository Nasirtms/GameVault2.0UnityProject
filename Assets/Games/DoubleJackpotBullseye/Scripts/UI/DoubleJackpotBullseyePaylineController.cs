using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;

[System.Serializable]
public class DoubleJackpotBullseyePaylineData
{
    public int paylineIndex;
    public Image lineImage;
    public Image leftIcon;
    public Image rightIcon;
}

[System.Serializable]
public class DoubleJackpotBullseyePaylineEntry
{
    private DoubleJackpotBullseyePaylineData payline;
    public DoubleJackpotBullseyePaylineEntry(DoubleJackpotBullseyePaylineData payline) { this.payline = payline; }
    public DoubleJackpotBullseyePaylineData PaylineData => payline;
}

[System.Serializable]
public class DoubleJackpotBullseyePaylineResult
{
    public int paylineNumber;
    public DoubleJackpotBullseyePaylineResult(int paylineNumber) { this.paylineNumber = paylineNumber; }
}

public class DoubleJackpotBullseyePaylineController : MonoBehaviour
{
    #region Variables

    public static DoubleJackpotBullseyePaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<DoubleJackpotBullseyePaylineData> paylines;

    private List<DoubleJackpotBullseyePaylineEntry> activePaylines = new();
    private readonly List<DoubleJackpotBullseyeSlotScript> animatedSlots = new();

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.5f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<DoubleJackpotBullseyePaylineResult> spinResults = new();

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

    public void AddPaylineResult(DoubleJackpotBullseyePaylineResult result)
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
            if (data != null) activePaylines.Add(new DoubleJackpotBullseyePaylineEntry(data));
        }

        if (activePaylines.Count == 0)
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
            DoubleJackpotBullseyeSlotMachine.Instance.isPaylineCompleted = true;
            if (DoubleJackpotBullseyeSlotMachine.Instance.isFreeGameReady)
            {
                DoubleJackpotBullseyeSlotMachine.Instance.isFreeGameReady = false;
                yield return PlayBullseyeBorderThenTransition();
            }
        }
    }

    #endregion

    #region Visual helpers

    private void Show(DoubleJackpotBullseyePaylineData visual)
    {
        SetAlpha(visual.lineImage, 1f);
        SetAlpha(visual.leftIcon, 1f);
        SetAlpha(visual.rightIcon, 1f);
    }

    private void Hide(DoubleJackpotBullseyePaylineData visual)
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

    #region Slot animation (trigger bullseye only)

    private void PlayWildAnimationsForPayline(int paylineIndex)
    {
        StopWildAnimationsOnCurrentLine(); // safety

        if (!paylinePatterns.TryGetValue(paylineIndex, out var rows))
        {
            // No pattern for this payline; nothing to animate.
            return;
        }

        // Expecting 3 reels; if you change reel count, update logic below.
        for (int reel = 0; reel < DoubleJackpotBullseyeSlotMachine.Instance.reels.Count; reel++)
        {
            int row = rows[Mathf.Clamp(reel, 0, rows.Length - 1)];
            var slot = DoubleJackpotBullseyeSlotMachine.Instance.reels[reel].slots[row + 1]; // [1..3] are visible
            if (slot == null) continue;

            //if (slot.type == DoubleJackpotBullseyeSlotType.DoubleJackpotBullseye)
            //{
                slot.StartAnimation();
                animatedSlots.Add(slot);
            //}
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

    private IEnumerator PlayBullseyeBorderThenTransition()
    {
        var sm = DoubleJackpotBullseyeSlotMachine.Instance;
        if (sm.reels.Count < 3) yield break;

        var middle = sm.reels[1];
        DoubleJackpotBullseyeSlotScript bullseyeSlot = null;

        for (int row = 0; row < 3; row++)
        {
            var s = middle.slots[row + 1]; // visible are 1..3
            if (s != null && s.type == DoubleJackpotBullseyeSlotType.DoubleJackpotBullseye)
            {
                bullseyeSlot = s; break;
            }
        }

        // Pulse the Bullseye border briefly (if found)
        if (bullseyeSlot != null)
        {
            bullseyeSlot.StartAnimation();
            yield return new WaitForSeconds(0.8f);
            bullseyeSlot.StopAnimation();
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // Start the free-spin transition now
        DoubleJackpotBullseyeFreeGameTransitionController.Instance.StartFreeSpinTransition();
    }
    #endregion
}
