using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
public class DayOfDeadPaylineController : MonoBehaviour
{
    #region Variables

    public static DayOfDeadPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<DayOfDeadPaylineData> paylines;

    public List<DayOfDeadPaylineEntry> activePaylines = new List<DayOfDeadPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<DayOfDeadPaylineResult> spinResult = new List<DayOfDeadPaylineResult>();
    private int resultScatterCount;

    //private GameObject overlay;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }
    #endregion

    #region Public References
    public void StartPayline(int freeSpinCount)
    {
        resultScatterCount = freeSpinCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(DayOfDeadPaylineResult result)
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

    private void StartPaylineDisplay(List<DayOfDeadPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new DayOfDeadPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3)
        {
            Debug.LogWarning("No valid paylines to display.");
            //overlay.SetActive(false);
            DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted = true;

            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public bool isJackotGame;
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
        //SaharaRichesUIManager.Instance.PlaySound("Payline");
        if (!DayOfDeadAutoSpinController.isAutoSpinning && !DayOfDeadSlotMachine.Instance.isFreeGame)
        {
            DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        if (DayOfDeadSlotMachine.Instance.isFreeGame || DayOfDeadSlotMachine.Instance.isReSpin || DayOfDeadAutoSpinController.isAutoSpinning)
        {
            flickerDelay = 1f;
        }
        else
        {
            flickerDelay = 2f;
        }
        //overlay.SetActive(true);

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }
        if (activePaylines.Count == 0)
        {
            DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                Invoke("PaylineAnimationCompleted", 2f);

            }
            else
            {
                int i = 0;
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        i++;
                        ResetAllSlotsToDefault();
                        yield return null;
                        yield return PlaySinglePayline(entry);
                    }
                    if (activePaylines.Count == i)
                    {
                        Invoke("PaylineAnimationCompleted", 2f);
                    }
                }
            }
        }

    }

    public void PaylineAnimationCompleted()
    {
        isShowing = false;
        DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(DayOfDeadPaylineEntry entry)
    {
        if (DayOfDeadSlotMachine.Instance.isRespinActive || DayOfDeadSlotMachine.Instance.isFreeGame)
        {
            flickerDelay = 1f;
        }
        for (int x = 0; x < DayOfDeadSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = DayOfDeadSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
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
        foreach (var reel in DayOfDeadSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.StopAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < DayOfDeadSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = DayOfDeadSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == DayOfDeadSlotType.Scatter)
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (!DayOfDeadSlotMachine.Instance.isFreeGame)
        {
            DayOfDeadSlotMachine.Instance.firstFreeSpin = true;
            DayOfDeadUIManager.Instance.UpdateButtons("Transition Start");
            DayOfDeadFreeGameTransitionController.Instance.StartFreeSpinTransition();
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class DayOfDeadPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[20]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 4]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 4; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class DayOfDeadPaylineEntry
{
    public DayOfDeadPaylineData payline;
    public int reelLimit;
    public string winText;
    public DayOfDeadPaylineEntry(DayOfDeadPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        //if (float.TryParse(winText, out float parsedValue))
        //{
        //    this.winText = parsedValue.ToString("F2");
        //}
        //else
        //{
        //    this.winText = winText;
        //}
    }
}

[System.Serializable]
public class DayOfDeadPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public DayOfDeadPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion