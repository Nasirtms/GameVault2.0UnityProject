using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
public class GoldRushGusPaylineController : MonoBehaviour
{
    #region Variables

    public static GoldRushGusPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<GoldRushGusPaylineData> paylines;
    // Currently running paylines
    public List<GoldRushGusPaylineEntry> activePaylines = new List<GoldRushGusPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true
    //[SerializeField] private GameObject overlay;
    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<GoldRushGusPaylineResult> spinResult = new List<GoldRushGusPaylineResult>();
    private bool resultfreeGameReady;
    private bool resultMiniGame;
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
        resultMiniGame = GoldRushGusSlotMachine.Instance.isMiniGameReady;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(GoldRushGusPaylineResult result)
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

    private void StartPaylineDisplay(List<GoldRushGusPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        //overlay.SetActive(true);
        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new GoldRushGusPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }
        if (activePaylines.Count == 0 && !resultfreeGameReady && !resultMiniGame)
        {
            //overlay.SetActive(false);
            GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted = true;
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
        //overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if ((activePaylines == null || activePaylines.Count == 0) && !resultfreeGameReady)
        {
            GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }
      
        if (activePaylines.Count == 0)
        {
            GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        if (resultfreeGameReady || resultMiniGame)
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
            if (GoldRushGusAutoSpinController.isAutoSpinning || GoldRushGusSlotMachine.Instance.isFreeGame || GoldRushGusSlotMachine.Instance.isMiniGameReady)
            {
                foreach (var entry in activePaylines)
                {
                    ResetAllSlotsToDefault();
                    yield return null;
                    yield return PlaySinglePayline(entry);
                }
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        ResetAllSlotsToDefault();
                        yield return null;
                        yield return PlaySinglePayline(entry);
                    }
                }
            }
        }
        GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(GoldRushGusPaylineEntry entry)
    {
        for (int x = 0; x < GoldRushGusSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = GoldRushGusSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
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
        foreach (var reel in GoldRushGusSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSpriteToDefault();
                    slot.StopAnimation();
                    slot.StopBorderAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < GoldRushGusSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = GoldRushGusSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if ((resultMiniGame && slot.slotType == GoldRushGusSlotType.Key))
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
                }
                if (GoldRushGusSlotMachine.Instance.freeSpinCount > 0 && slot.slotType == GoldRushGusSlotType.FreeSpin)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
                }
            }
        }
        //yield return new WaitForSeconds(1f);
        if (GoldRushGusSlotMachine.Instance.freeSpinCount > 0 && !GoldRushGusSlotMachine.Instance.isFreeGame)
        {
            GoldRushGusSlotMachine.Instance.firstFreeSpin = true;
            GoldRushGusUIManager.Instance.UpdateButtons("Transition Start");
            GoldRushGusFreeGameTransitionController.Instance.StartFreeSpinTransition();
            GoldRushGusFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldRushGusSlotMachine.Instance.freeSpinCount);
        }
        else if (GoldRushGusSlotMachine.Instance.freeSpinCount > 0 && GoldRushGusSlotMachine.Instance.isFreeGame)
        {
            GoldRushGusFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldRushGusSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class GoldRushGusPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]); 

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 3; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class GoldRushGusPaylineEntry
{
    public GoldRushGusPaylineData payline;
    public int reelLimit;
    public string winText;
    public GoldRushGusPaylineEntry(GoldRushGusPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        if (float.TryParse(winText, out float parsedValue))
        {
            this.winText = parsedValue.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class GoldRushGusPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public GoldRushGusPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion