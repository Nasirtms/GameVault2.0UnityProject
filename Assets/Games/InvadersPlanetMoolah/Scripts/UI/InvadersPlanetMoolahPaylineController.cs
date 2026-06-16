using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class InvadersPlanetMoolahPaylineController : MonoBehaviour
{
    #region Variables

    public static InvadersPlanetMoolahPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<InvadersPlanetMoolahPaylineData> paylines;

    public List<InvadersPlanetMoolahPaylineEntry> activePaylines = new List<InvadersPlanetMoolahPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;

    private Coroutine animationLoop;
    private Coroutine scatterAnimation;

    public bool isShowing = false;

    //[SerializeField] private GameObject overlay;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<InvadersPlanetMoolahPaylineResult> spinResult = new List<InvadersPlanetMoolahPaylineResult>();

    private bool resultfreeGameReady;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;
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

    public void AddPaylineData(InvadersPlanetMoolahPaylineResult result)
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

    private void StartPaylineDisplay(List<InvadersPlanetMoolahPaylineResult> results)
    {
        StopPaylineDisplay();
        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);

            if (paylineData != null)
            {
                activePaylines.Add(new InvadersPlanetMoolahPaylineEntry(paylineData,result.reelLimit,result.winText));
            }
        }

        if (activePaylines.Count == 0 && !resultfreeGameReady)
        {
            InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted = true;
            //overlay.SetActive(false);
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
            InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 0)
        {
            InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        //overlay.SetActive(true);

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
            if (InvadersPlanetMoolahAutoSpinController.isAutoSpinning || InvadersPlanetMoolahSlotMachine.Instance.isFreeGame)
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

        InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(InvadersPlanetMoolahPaylineEntry entry)
    {
        for (int x = 0; x < InvadersPlanetMoolahSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = InvadersPlanetMoolahSlotMachine.Instance.reels[x].slots[y + 1];

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
        foreach (var reel in InvadersPlanetMoolahSlotMachine.Instance.reels)
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
        //for (int x = 0; x < InvadersPlanetMoolahSlotMachine.Instance.reels.Count; x++)
        //{
        //    for (int y = 0; y < 3; y++)
        //    {
        //        var slot = InvadersPlanetMoolahSlotMachine.Instance.reels[x].slots[y + 1];

        //        if (slot == null) continue;

        //        if (InvadersPlanetMoolahSlotMachine.Instance.freeSpinCount > 0 &&
        //            slot.slotType == InvadersPlanetMoolahSlotType.GoldCoin)
        //        {
        //            slot.SetSpriteToPayline();
        //            slot.PlayAnimation();
        //        }
        //    }
        //}

        if (InvadersPlanetMoolahSlotMachine.Instance.freeSpinCount > 0 &&
            !InvadersPlanetMoolahSlotMachine.Instance.isFreeGame)
        {
            InvadersPlanetMoolahSlotMachine.Instance.firstFreeSpin = true;

            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Transition Start");

            //InvadersPlanetMoolahFreeGameTransitionController.Instance.StartFreeSpinTransition();
            //InvadersPlanetMoolahFreeGameTransitionController.Instance.UpdateFreeSpinsCount(
            //    InvadersPlanetMoolahSlotMachine.Instance.freeSpinCount
            //);
        }
        else if (InvadersPlanetMoolahSlotMachine.Instance.freeSpinCount > 0 &&
                 InvadersPlanetMoolahSlotMachine.Instance.isFreeGame)
        {
            //InvadersPlanetMoolahFreeGameTransitionController.Instance.UpdateFreeSpinsCount(
            //    InvadersPlanetMoolahSlotMachine.Instance.freeSpinCount
            //);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class InvadersPlanetMoolahPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]);

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3];

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }

        return matrix;
    }
}

public class InvadersPlanetMoolahPaylineEntry
{
    public InvadersPlanetMoolahPaylineData payline;
    public int reelLimit;
    public string winText;

    public InvadersPlanetMoolahPaylineEntry(InvadersPlanetMoolahPaylineData payline, int reelLimit, string winText)
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
public class InvadersPlanetMoolahPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public InvadersPlanetMoolahPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion