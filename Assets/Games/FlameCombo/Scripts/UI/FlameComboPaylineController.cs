using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FlameComboPaylineController : MonoBehaviour
{
    #region Variables

    public static FlameComboPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<FlameComboPaylineData> paylines;

    // Currently running paylines
    public List<FlameComboPaylineEntry> activePaylines = new();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<FlameComboPaylineResult> spinResult = new List<FlameComboPaylineResult>();
    private int resultScatterCount;
    private Coroutine scatterAnimation;
    public int Hit_Count = 0;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void AddPaylineData(FlameComboPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }
    public void ClearPaylineData()
    {
        StopPaylineDisplay();

        foreach (var entry in activePaylines)
        {
            for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        slot.StopAnimation();
                    }
                }
            }
        }
        for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FlameComboSlotType.HIT || slot.type == FlameComboSlotType.FREE_GAME)
                {
                    slot.StopAnimation();
                }
            }
        }

        spinResult.Clear();
        activePaylines.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<FlameComboPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new FlameComboPaylineEntry(
                    paylineData, result.reelLimit
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3 && Hit_Count < 3)
        {
            Debug.LogWarning("No valid paylines to display.");
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void ShowCollectedPaylines(int scatterCount)
    {
        Hit_Count = 0;
        resultScatterCount = scatterCount;
        Hit_Count = FlameComboSlotMachine.Instance.hit_Count;
        if ((spinResult == null || spinResult.Count == 0) && resultScatterCount < 3)
        {
            FlameComboSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }

        StartPaylineDisplay(spinResult);
    }
    private void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        if (paylines != null)
        {
            foreach (var visual in paylines)
                Hide(visual);
        }
    }

    #region Payline Visual helpers

    private void Show(FlameComboPaylineData visual)
    {
        SetAlpha(visual.paylineImage, 1f);
    }

    private void Hide(FlameComboPaylineData visual)
    {
        SetAlpha(visual.paylineImage, 0f);
    }
    private void SetAlpha(Image img, float alpha)
    {
        if (!img) return;
        var c = img.color;
        c.a = alpha; 
        img.color = c;
    }

    #endregion

    private IEnumerator PlayPaylines()
    {
        if (activePaylines == null && activePaylines.Count == 0 && resultScatterCount < 3)
        {
            FlameComboSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }

        if (!FlameComboAutoSpinController.isAutoSpinning && !FlameComboSlotMachine.Instance.isFreeGame && resultScatterCount < 3)
        {
            FlameComboSlotMachine.Instance.isPaylineCompleted = true;
        }

        if (resultScatterCount >= 3 || Hit_Count >= 3 )
        {
            yield return ScatterAnimation();
        }

        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline();
        }
        else
        {
            if (FlameComboAutoSpinController.isAutoSpinning || FlameComboSlotMachine.Instance.isFreeGame)
            {
                yield return PlaySinglePayline();

            }
            else
            {
                while (isShowing)
                {
                    yield return PlaySinglePayline();

                }
            }
        }
        FlameComboSlotMachine.Instance.isPaylineCompleted = true;
    }
    private IEnumerator PlaySinglePayline()
    {
        int index = 0;
        int maxIndex = activePaylines.Count;

        while (true)
        {
            if (activePaylines == null || activePaylines.Count == 0)
                yield break;

            foreach (var pl in activePaylines)
            {
                Hide(pl.payline);
                for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (pl.payline.ToMatrix()[x, y] == 1 && x < pl.reelLimit)
                        {
                            if (y + 1 >= FlameComboSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                            if (slot == null) continue;

                            if (pl.payline.ToMatrix()[x, y] == 1)
                            {
                                slot.StopAnimation();
                            }
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.3f);

            if (activePaylines.Count == 0) yield break;

            index = index % activePaylines.Count;
            var currentPayline = activePaylines[index];

            Show(currentPayline.payline);

            for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (currentPayline.payline.ToMatrix()[x, y] == 1 && x < currentPayline.reelLimit)
                    {
                        if (y + 1 >= FlameComboSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (currentPayline.payline.ToMatrix()[x, y] == 1)
                        {
                            slot.PlayAnimation();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(1.8f);
            if (index + 1 == maxIndex && (FlameComboAutoSpinController.isAutoSpinning || FlameComboSlotMachine.Instance.isFreeGame))
            {
                FlameComboSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            index++;
        }
    }
    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if ((Hit_Count >= 3 && slot.type == FlameComboSlotType.HIT) || (FlameComboSlotMachine.Instance.isFreeGameReady && 
                    resultScatterCount >= 3 && slot.type == FlameComboSlotType.FREE_GAME))
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (FlameComboSlotMachine.Instance.freeSpinCount > 0 && !FlameComboSlotMachine.Instance.isFreeGame)
        {
            FlameComboSlotMachine.Instance.firstFreeSpin = true;
            FlameComboUIManager.Instance.UpdateButtons("Transition Start");
            FlameComboFreeGameTransitionController.Instance.StartFreeSpinTransition();
            FlameComboFreeGameTransitionController.Instance.UpdateFreeSpinsCount(FlameComboSlotMachine.Instance.freeSpinCount);
        }
        else if (FlameComboSlotMachine.Instance.freeSpinCount > 0)
        {
            FlameComboFreeGameTransitionController.Instance.UpdateFreeSpinsCount(FlameComboSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            FlameComboSlotMachine.Instance.isPaylineCompleted = true;
        }

        for (int x = 0; x < FlameComboSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FlameComboSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FlameComboSlotType.HIT || slot.type == FlameComboSlotType.FREE_GAME)
                {
                    slot.StopAnimation();
                }
            }
        }
    }
    #endregion
}

#region Support Classes

[System.Serializable]
public class FlameComboPaylineData
{
    public int paylineNumber;
    public Image paylineImage;

    public List<int> pattern = new List<int>(new int[15]); // 5 * 3

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3];
        for (int x = 0; x < 5; x++) // columns
        {
            for (int y = 0; y < 3; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class FlameComboPaylineEntry
{
    public FlameComboPaylineData payline;
    public int reelLimit;

    public FlameComboPaylineEntry(FlameComboPaylineData payline, int reelLimit)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
    }
}

[System.Serializable]
public class FlameComboPaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public FlameComboPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}

#endregion