using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class SecretsOfChristmasPaylineController : MonoBehaviour
{
    #region Variables

    public static SecretsOfChristmasPaylineController Instance;

    [Header("Paylines")]
    [SerializeField]
    private List<SecretsOfChristmasPaylineData> paylines = new()
    {
        new SecretsOfChristmasPaylineData(1,  new[] { 1, 1, 1, 1, 1 }),
        new SecretsOfChristmasPaylineData(2,  new[] { 0, 0, 0, 0, 0 }),
        new SecretsOfChristmasPaylineData(3,  new[] { 2, 2, 2, 2, 2 }),
        new SecretsOfChristmasPaylineData(4,  new[] { 0, 1, 2, 1, 0 }),
        new SecretsOfChristmasPaylineData(5,  new[] { 2, 1, 0, 1, 2 }),

        new SecretsOfChristmasPaylineData(6,  new[] { 0, 0, 1, 0, 0 }),
        new SecretsOfChristmasPaylineData(7,  new[] { 2, 2, 1, 2, 2 }),
        new SecretsOfChristmasPaylineData(8,  new[] { 1, 2, 2, 2, 1 }),
        new SecretsOfChristmasPaylineData(9,  new[] { 1, 0, 0, 0, 1 }),
        new SecretsOfChristmasPaylineData(10, new[] { 1, 0, 1, 0, 1 }),

        new SecretsOfChristmasPaylineData(11, new[] { 1, 2, 1, 2, 1 }),
        new SecretsOfChristmasPaylineData(12, new[] { 0, 1, 0, 1, 0 }),
        new SecretsOfChristmasPaylineData(13, new[] { 2, 1, 2, 1, 2 }),
        new SecretsOfChristmasPaylineData(14, new[] { 1, 1, 0, 1, 1 }),
        new SecretsOfChristmasPaylineData(15, new[] { 1, 1, 2, 1, 1 }),

        new SecretsOfChristmasPaylineData(16, new[] { 0, 1, 1, 1, 0 }),
        new SecretsOfChristmasPaylineData(17, new[] { 2, 1, 1, 1, 2 }),
        new SecretsOfChristmasPaylineData(18, new[] { 0, 2, 0, 2, 0 }),
        new SecretsOfChristmasPaylineData(19, new[] { 2, 0, 2, 0, 2 }),
        new SecretsOfChristmasPaylineData(20, new[] { 0, 2, 2, 2, 0 }),

        new SecretsOfChristmasPaylineData(21, new[] { 2, 0, 0, 0, 2 }),
        new SecretsOfChristmasPaylineData(22, new[] { 0, 0, 2, 0, 0 }),
        new SecretsOfChristmasPaylineData(23, new[] { 2, 2, 0, 2, 2 }),
        new SecretsOfChristmasPaylineData(24, new[] { 0, 2, 1, 2, 0 }),
        new SecretsOfChristmasPaylineData(25, new[] { 2, 0, 1, 0, 2 }),
    };

    public List<SecretsOfChristmasPaylineEntry> activePaylines = new List<SecretsOfChristmasPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;
    [SerializeField] private GameObject overlay;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<SecretsOfChristmasPaylineResult> spinResult = new List<SecretsOfChristmasPaylineResult>();
    private bool resultfreeGameReady;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References

    public void StartPayline(bool freeSpinReady)
    {
        resultfreeGameReady = freeSpinReady;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(SecretsOfChristmasPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        spinResult.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<SecretsOfChristmasPaylineResult> results)
    {
        StopPaylineDisplay();
        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new SecretsOfChristmasPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && !resultfreeGameReady)
        {
            SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted = true;
            overlay.SetActive(false);
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    private void StopPaylineDisplay()
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

        ResetAllSlotsToDefault();
        overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if ((activePaylines == null || activePaylines.Count == 0) && !resultfreeGameReady)
        {
            SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 0)
        {
            SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        overlay.SetActive(true);

        if (resultfreeGameReady)
        {
            if (scatterAnimation != null)
            {
                StopCoroutine(scatterAnimation);
            }

            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline(activePaylines[0]);
        }
        else
        {
            if (SecretsOfChristmasAutoSpinController.isAutoSpinning || SecretsOfChristmasSlotMachine.Instance.isFreeGame)
            {
                foreach (var entry in activePaylines)
                {
                    yield return PlaySinglePayline(entry);
                }
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }
                }
            }
        }

        SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(SecretsOfChristmasPaylineEntry entry)
    {
        for (int x = 0; x < SecretsOfChristmasSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = SecretsOfChristmasSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }

    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in SecretsOfChristmasSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSpriteToDefault();
                    slot.StopAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < SecretsOfChristmasSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = SecretsOfChristmasSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (SecretsOfChristmasSlotMachine.Instance.freeSpinCount > 0 && slot.slotType == SecretsOfChristmasSlotType.Scatter)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        if (SecretsOfChristmasSlotMachine.Instance.freeSpinCount > 0 && !SecretsOfChristmasSlotMachine.Instance.isFreeGame)
        {
            SecretsOfChristmasSlotMachine.Instance.firstFreeSpin = true;
            SecretsOfChristmasUIManager.Instance.UpdateButtons("Transition Start");
            //SecretsOfChristmasFreeGameTransitionController.Instance.StartFreeSpinTransition();
            //SecretsOfChristmasFreeGameTransitionController.Instance.UpdateFreeSpinsCount(SecretsOfChristmasSlotMachine.Instance.freeSpinCount);
        }
        else if (SecretsOfChristmasSlotMachine.Instance.freeSpinCount > 0 && SecretsOfChristmasSlotMachine.Instance.isFreeGame)
        {
            //SecretsOfChristmasFreeGameTransitionController.Instance.UpdateFreeSpinsCount(IrishPotLuckSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class SecretsOfChristmasPaylineData
{
    public int paylineNumber;

    // 5 values only, one row per reel.
    // 0 = top, 1 = second, 2 = third
    public List<int> rows = new List<int>(5);

    public SecretsOfChristmasPaylineData(int paylineNumber, int[] rows)
    {
        this.paylineNumber = paylineNumber;
        this.rows = new List<int>(rows);
    }

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3];

        for (int x = 0; x < 5; x++)
        {
            int y = rows[x];

            if (y >= 0 && y < 3)
                matrix[x, y] = 1;
        }

        return matrix;
    }
}

public class SecretsOfChristmasPaylineEntry
{
    public SecretsOfChristmasPaylineData payline;
    public int reelLimit;
    public string winText;

    public SecretsOfChristmasPaylineEntry(SecretsOfChristmasPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;

        if (float.TryParse(winText, out float parsedValue))
        {
            float floored = Mathf.Floor(parsedValue * 100f) / 100f;
            this.winText = floored.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class SecretsOfChristmasPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public SecretsOfChristmasPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion