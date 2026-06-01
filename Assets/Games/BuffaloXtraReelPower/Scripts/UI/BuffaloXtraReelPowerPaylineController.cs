using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffaloXtraReelPowerPaylineController : MonoBehaviour
{
    #region Variables

    public static BuffaloXtraReelPowerPaylineController Instance;

    // Currently running paylines
    private List<BuffaloXtraReelPowerPaylineResult> activePaylines = new List<BuffaloXtraReelPowerPaylineResult>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<BuffaloXtraReelPowerPaylineResult> spinResult = new List<BuffaloXtraReelPowerPaylineResult>();
    private int resultScatterCount;

    //[SerializeField] private GameObject overlay;

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

    public void AddPaylineData(BuffaloXtraReelPowerPaylineResult result)
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

    private void StartPaylineDisplay(List<BuffaloXtraReelPowerPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        //overlay.SetActive(true);

        foreach (var result in results)
        {
            activePaylines.Add(result);
        }

        if (activePaylines.Count == 0 && resultScatterCount > 3)
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

        if (scatterAnimation != null)
        {
            StopCoroutine(scatterAnimation);
            scatterAnimation = null;
        }

        foreach (var reel in BuffaloXtraReelPowerSlotMachine.Instance.reels)
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

        //overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if (!BuffaloXtraReelPowerAutoSpinController.isAutoSpinning && !BuffaloXtraReelPowerSlotMachine.Instance.isFreeGame)
        {
            BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted = true;

                    //if ((ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted && ZombieParadiseAutoSpinController.isAutoSpinning) || ZombieParadiseSlotMachine.Instance.isFreeGame)
                    //{
                    //    break;
                    //}
                }
            }
        }

    }

    private IEnumerator PlaySinglePayline(BuffaloXtraReelPowerPaylineResult entry)
    {
        float waitTime = flickerDelay;
        int paylineLimit = entry.slots.Count;

        for (int x = 0; x < BuffaloXtraReelPowerSlotMachine.Instance.reels.Count; x++)
        {
            if (x >= paylineLimit) break;

            for (int y = 0; y < 4; y++)
            {
                var slot = BuffaloXtraReelPowerSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.slots[x] == y)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            foreach (var reel in BuffaloXtraReelPowerSlotMachine.Instance.reels)
            {
                foreach (var slot in reel.slots)
                {
                    if (slot != null)
                    {
                        //slot.SetSpriteToDefault();
                        slot.StopAnimation();
                    }
                }
            }
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < BuffaloXtraReelPowerSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = BuffaloXtraReelPowerSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == BuffaloXtraReelPowerSlotType.Scatter)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        if (BuffaloXtraReelPowerSlotMachine.Instance.freeSpinCount > 0 && !BuffaloXtraReelPowerSlotMachine.Instance.isFreeGame)
        {
            BuffaloXtraReelPowerSlotMachine.Instance.firstFreeSpin = true;
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Transition Start");
            //BuffaloXtraReelPowerFreeGameTransitionController.Instance.StartFreeSpinTransition();
            //BuffaloXtraReelPowerFreeGameTransitionController.Instance.UpdateFreeSpinsCount(BuffaloXtraReelPowerSlotMachine.Instance.freeSpinCount);
        }
        else if (BuffaloXtraReelPowerSlotMachine.Instance.freeSpinCount > 0)
        {
            //BuffaloXtraReelPowerFreeGameTransitionController.Instance.UpdateFreeSpinsCount(BuffaloXtraReelPowerSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class BuffaloXtraReelPowerPaylineResult
{
    public List<int> slots;

    public BuffaloXtraReelPowerPaylineResult(List<int> slotNumbers)
    {
        this.slots = slotNumbers;
    }
}

#endregion