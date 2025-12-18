using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CleopatraPaylineController : MonoBehaviour
{
    #region Variables

    public static CleopatraPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<CleopatraPaylineData> paylines;

    // Currently running paylines
    private List<CleopatraPaylineEntry> activePaylines = new List<CleopatraPaylineEntry>();

    [Header("Scatter")]
    [SerializeField] private Color scatterColor;

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<CleopatraPaylineResult> spinResult = new List<CleopatraPaylineResult>();
    private int resultScatterCount;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References

    public void StartPayline(int scatterCount)
    {
        resultScatterCount = scatterCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(CleopatraPaylineResult result)
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

    private void StartPaylineDisplay(List<CleopatraPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new CleopatraPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 2)
        {
            Debug.LogWarning("No valid paylines to display.");
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

        foreach (var reel in CleopatraSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSortingLayer(0, false);
                    slot.ResetAnimator();
                    slot.HideAllVisualOverlays();
                }
            }
        }

        foreach (var payline in activePaylines)
        {
            if (payline.payline.paylineSprite.activeSelf == true)
                payline.payline.paylineSprite.SetActive(false);
        }
    }

    private IEnumerator PlayPaylines()
    {
        if (!CleopatraAutoSpinController.isAutoSpinning && !CleopatraSlotMachine.Instance.isFreeGame)
        {
            CleopatraSlotMachine.Instance.isPaylineCompleted = true;
        }

        if (activePaylines.Count > 0)
        {
            foreach (var payline in activePaylines)
            {
                payline.payline.paylineSprite.SetActive(true);
                payline.payline.paylineSprite.GetComponent<Image>().color = payline.payline.paylineColor;
            }

            yield return new WaitForSeconds(1f);
        }

        if (activePaylines.Count > 0)
        {
            foreach (var payline in activePaylines)
            {
                payline.payline.paylineSprite.SetActive(false);
            }
        }

        if (resultScatterCount >= 2)
        {
            ScatterAnimation(resultScatterCount);
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                CleopatraSlotMachine.Instance.isPaylineCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    CleopatraSlotMachine.Instance.isPaylineCompleted = true;

                    if ((CleopatraSlotMachine.Instance.isPaylineCompleted && CleopatraAutoSpinController.isAutoSpinning) || CleopatraSlotMachine.Instance.isFreeGame)
                    {
                        break;
                    }
                }
            }
        }
        
    }

    private IEnumerator PlaySinglePayline(CleopatraPaylineEntry entry)
    {
        float waitTime = flickerDelay;

        if (entry.payline.paylineSprite != null)
        {
            entry.payline.paylineSprite.SetActive(true);
        }

        bool hasAnimation = false;

        for (int x = 0; x < CleopatraSlotMachine.Instance.reels.Count; x++)
        {
            //if (x >= entry.reelLimit) break;

            for (int y = 0; y < 3; y++)
            {
                var slot = CleopatraSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {

                    slot.SetSortingLayer(1, true);

                    try
                    {
                        slot.SetBorderColor(entry.payline.paylineColor);
                    }
                    catch
                    {
                        slot.SetBorderColor(Color.white);
                    }

                    slot.SetBorderVisible(true);

                    if (x == entry.reelLimit - 1)
                    {
                        slot.SetTextGroupVisible(true);
                        slot.SetWinText(entry.winText);
                    }

                    if (slot.type == CleopatraSlotType.Cleopatra)
                    {
                        hasAnimation = true;
                        slot.PlayAnimation("Double");
                        waitTime = slot.GetClipLengthByName("CleopatraSlotDoubleAnimation");
                    }
                }
                else
                {
                    if (!(resultScatterCount >= 2 && slot.type == CleopatraSlotType.Sphinx))
                        slot.SetOverlayVisible(true);
                }
            }
        }

        if (hasAnimation)
        {
            // Wait for the animation duration (optional to replace with animation event)
            //Debug.Log(waitTime);
            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            //Debug.Log(waitTime);
            yield return new WaitForSeconds(waitTime);
        }

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            foreach (var reel in CleopatraSlotMachine.Instance.reels)
            {
                foreach (var slot in reel.slots)
                {
                    if (slot != null && slot.type != CleopatraSlotType.Sphinx)
                    {
                        slot.SetSortingLayer(0, false);
                        slot.HideAllVisualOverlays();
                        slot.ResetAnimator();
                    }
                }
            }

            if (entry.payline.paylineSprite != null)
                entry.payline.paylineSprite.SetActive(false);
        }
    }

    private void ScatterAnimation(int scatterCount)
    {
        int count = 0;

        for (int x = 0; x < CleopatraSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = CleopatraSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == CleopatraSlotType.Sphinx)
                {
                    count++;

                    slot.SetSortingLayer(1, true);

                    slot.SetBorderVisible(true);
                    slot.SetBorderColor(scatterColor);

                    if (count == scatterCount && scatterCount == 2)
                    {
                        slot.SetTextGroupVisible(true);
                        slot.SetWinText((CleopatraUIManager.Instance.CurrentBet() * 2).ToString("N2"));
                    }

                    string trigger = scatterCount == 2 ? "Scatter" : "Bonus";
                    slot.PlayAnimation(trigger);
                }
            }
        }

        if (scatterCount > 2)
        {
            if (!CleopatraSlotMachine.Instance.isFreeGame)
            {
                CleopatraUIManager.Instance.PlaySound("Bonus");
                CleopatraUIManager.Instance.ShowFreeSpinIndicator();
                CleopatraUIManager.Instance.UpdateButtons("Free Spin");
            }
            else
            {
                CleopatraUIManager.Instance.ExtraFreeSpin();
            }
        }

        if (spinResult.Count < 1)
        {
            CleopatraSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    #endregion
}
