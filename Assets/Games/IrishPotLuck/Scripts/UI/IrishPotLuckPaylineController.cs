using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class IrishPotLuckPaylineController : MonoBehaviour
{
    #region Variables

    public static IrishPotLuckPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<IrishPotLuckPaylineData> paylines;
    // Currently running paylines
    public List<IrishPotLuckPaylineEntry> activePaylines = new List<IrishPotLuckPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;
    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<IrishPotLuckPaylineResult> spinResult = new List<IrishPotLuckPaylineResult>();
    private bool resultfreeGameReady;
    private bool resultJackpotGameReady;
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
        resultJackpotGameReady = IrishPotLuckSlotMachine.Instance.isJackpotGameReady;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(IrishPotLuckPaylineResult result)
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

    private void StartPaylineDisplay(List<IrishPotLuckPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new IrishPotLuckPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }
        if (activePaylines.Count == 0 && !resultfreeGameReady && !resultJackpotGameReady)
        {
            IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted = true;
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
    }

    private IEnumerator PlayPaylines()
    {
        if ((activePaylines == null || activePaylines.Count == 0) && !resultfreeGameReady  && !resultJackpotGameReady)
        {
            IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 0)
        {
            IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline(activePaylines[0]);
        }
        else
        {
            if (IrishPotLuckAutoSpinController.isAutoSpinning || IrishPotLuckSlotMachine.Instance.isFreeGame || resultJackpotGameReady)
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
        if (resultfreeGameReady || resultJackpotGameReady)
        {
            if (scatterAnimation != null)
            {
                StopCoroutine(scatterAnimation);
            }
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }
        IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(IrishPotLuckPaylineEntry entry)
    {
        for (int x = 0; x < IrishPotLuckSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = IrishPotLuckSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    //slot.PlayAnimation();
                    bool spawnedAnimated = IrishPotLuckSlotMachine.Instance.SetSpawnedSlotPaylineAnimation(slot, true);

                    if (!spawnedAnimated)
                    {
                        slot.PlayAnimation();
                    }
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
        foreach (var reel in IrishPotLuckSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    IrishPotLuckSlotMachine.Instance.SetSpawnedSlotPaylineAnimation(slot, false);
                    slot.StopAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < IrishPotLuckSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = IrishPotLuckSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (resultfreeGameReady)
                {
                    if (IrishPotLuckSlotMachine.Instance.freeSpinCount > 0 && slot.slotType == IrishPotLuckSlotType.Scatter)
                    {
                        slot.PlayAnimation();
                    }
                }
                if (resultJackpotGameReady)
                {
                    if(slot.slotType == IrishPotLuckSlotType.Jackpot)
                    {
                        slot.PlayAnimation();
                    }
                }
            }
        }

        if (resultJackpotGameReady)
        {
            //Debug.Log("LovKumar 3");
            IrishPotLuckUIManager.Instance.UpdateButtons("Transition Start");
            IrishPotLuckJackpotWheelTransition.Instance.StartJackpotWheelTransition();
        }

        //Debug.Log("LovKumar 7");
        if (IrishPotLuckSlotMachine.Instance.freeSpinCount > 0 && !IrishPotLuckSlotMachine.Instance.isFreeGame)
        {
            IrishPotLuckSlotMachine.Instance.firstFreeSpin = true;
            IrishPotLuckUIManager.Instance.UpdateButtons("Transition Start");
            IrishPotLuckFreeGameTransitionController.Instance.StartFreeSpinTransition();
            IrishPotLuckFreeGameTransitionController.Instance.UpdateFreeSpinsCount(IrishPotLuckSlotMachine.Instance.freeSpinCount);
        }
        else if (IrishPotLuckSlotMachine.Instance.freeSpinCount > 0 && IrishPotLuckSlotMachine.Instance.isFreeGame)
        {
            IrishPotLuckFreeGameTransitionController.Instance.UpdateFreeSpinsCount(IrishPotLuckSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class IrishPotLuckPaylineData
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

public class IrishPotLuckPaylineEntry
{
    public IrishPotLuckPaylineData payline;
    public int reelLimit;
    public string winText;
    public IrishPotLuckPaylineEntry(IrishPotLuckPaylineData payline, int reelLimit, string winText)
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
public class IrishPotLuckPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public IrishPotLuckPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion