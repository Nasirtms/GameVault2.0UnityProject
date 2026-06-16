using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CashVaultPaylineController : MonoBehaviour
{
    #region Variables

    public static CashVaultPaylineController Instance;

    // Currently running paylines
    private List<CashVaultPaylineResult> activePaylines = new List<CashVaultPaylineResult>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<CashVaultPaylineResult> spinResult = new List<CashVaultPaylineResult>();
    public int resultScatterCount;

    public GameObject overlay;

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

    public void AddPaylineData(CashVaultPaylineResult result)
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

    private void StartPaylineDisplay(List<CashVaultPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        overlay.SetActive(true);

        foreach (var result in results)
        {
            activePaylines.Add(result);
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3)
        {
            overlay.SetActive(false);
            //Debug.LogWarning("No valid paylines to display.");
            CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
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

        foreach (var reel in CashVaultSlotMachine.Instance.reels)
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
        ResetAllSlotsToDefault();
        overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if (!CashVaultAutoSpinController.isAutoSpinning && !CashVaultSlotMachine.Instance.isFreeGame)
        {
            CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        if (CashVaultSlotMachine.Instance.isBlindFeature) //resultScatterCount >= 3 
        {
            Debug.Log("Lov Kumar 0");
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }
        if (!CashVaultSlotMachine.Instance.isBlindFeature && resultScatterCount != 3 && resultScatterCount >=6)
        {
            Debug.Log("Lov Kumar 1");
            scatterAnimation = StartCoroutine(SphereAnimation());
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
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

                    CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;

                    //if ((ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted && ZombieParadiseAutoSpinController.isAutoSpinning) || ZombieParadiseSlotMachine.Instance.isFreeGame)
                    //{
                    //    break;
                    //}
                }
            }
        }
    }

    private IEnumerator PlaySinglePayline(CashVaultPaylineResult entry)
    {
        float waitTime;
        if (CashVaultAutoSpinController.isAutoSpinning || CashVaultSlotMachine.Instance.isFreeGame)
        {
            waitTime = 1.5f;
        }
        else
        {
            waitTime = flickerDelay;
        }
        int paylineLimit = entry.slots.Count;

        for (int x = 0; x < CashVaultSlotMachine.Instance.reels.Count; x++)
        {
            if (x >= paylineLimit) break;

            for (int y = 0; y < 3; y++)
            {
                var slot = CashVaultSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.slots[x] == y)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in CashVaultSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    //slot.SetSpriteToDefault();
                    slot.StopAnimation();
                    slot.StopBorderAnimation();
                }
            }
        }
    }
    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < CashVaultSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = CashVaultSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == CashVaultSlotType.Scatter)
                {
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
                }
            }
        }

        yield return new WaitUntil(() => CashVaultUIManager.Instance.winAnimation);

        if (CashVaultSlotMachine.Instance.isBlindFeature)
        {
            CashVaultBlindFeature.Instance.OnBlindFeatureCompleted -= ContinueAfterBlindFeature;
            CashVaultBlindFeature.Instance.OnBlindFeatureCompleted += ContinueAfterBlindFeature;
            if (!CashVaultSlotMachine.Instance.isFreeGame)
            {
                CashVaultBlindFeature.Instance.StartBlindFeatureTransition();
                yield break;
            }
            else if (CashVaultSlotMachine.Instance.freeSpinCount > 0)
            {
                CashVaultFreeGameTransitionController.Instance.UpdateFreeSpinsCount(CashVaultSlotMachine.Instance.freeSpinCount);
            }
        }
        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
            CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private void ContinueAfterBlindFeature()
    {
        CashVaultBlindFeature.Instance.OnBlindFeatureCompleted -= ContinueAfterBlindFeature;

        Debug.Log("Blind Feature Completed → Continuing Flow");

        if (!CashVaultSlotMachine.Instance.isMiniGame && CashVaultSlotMachine.Instance.isMiniGameReady)
        {
            Debug.Log("Lov Kumar 3");
            StartMiniGameFlow();
        }

        if (CashVaultSlotMachine.Instance.freeSpinCount > 0 && !CashVaultSlotMachine.Instance.isFreeGame)
        {
            Debug.Log("Lov Kumar 4");
            CashVaultSlotMachine.Instance.firstFreeSpin = true;
            CashVaultUIManager.Instance.UpdateButtons("Transition Start");
            CashVaultFreeGameTransitionController.Instance.StartFreeSpinTransition();
            CashVaultFreeGameTransitionController.Instance.UpdateFreeSpinsCount(CashVaultSlotMachine.Instance.freeSpinCount);
        }
        else if (CashVaultSlotMachine.Instance.freeSpinCount > 0)
        {
            CashVaultFreeGameTransitionController.Instance.UpdateFreeSpinsCount(CashVaultSlotMachine.Instance.freeSpinCount);
        }

        StartCoroutine(FinishScatterAfterBlindFeature());
    }
    private IEnumerator FinishScatterAfterBlindFeature()
    {
        Debug.Log("Lov Kumar 11");

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
            CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator SphereAnimation()
    {
        for (int x = 0; x < CashVaultSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = CashVaultSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == CashVaultSlotType.Sphere)
                {
                    slot.PlayAnimation();
                    slot.PlayBorderAnimation();
                }
            }
        }

        yield return new WaitUntil(() => CashVaultUIManager.Instance.winAnimation);
        Debug.Log("Lov Kumar 4");
        if (!CashVaultSlotMachine.Instance.isMiniGame && CashVaultSlotMachine.Instance.isMiniGameReady)
        {
            Debug.Log("Lov Kumar 5");
            StartMiniGameFlow();
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            CashVaultSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }
    private void StartMiniGameFlow()
    {
        overlay.SetActive(false);
        CashVaultMiniGameSlotMachine.Instance.firstReSpin = true;
        CashVaultUIManager.Instance.UpdateButtons("Transition Start");
        CashVaultMiniGameManager.Instance.StartReSpinTransition();
    }
    #endregion
}

#region Support Classes

[System.Serializable]
public class CashVaultPaylineResult
{
    public List<int> slots;

    public CashVaultPaylineResult(List<int> slotNumbers)
    {
        this.slots = slotNumbers;
    }
}

#endregion