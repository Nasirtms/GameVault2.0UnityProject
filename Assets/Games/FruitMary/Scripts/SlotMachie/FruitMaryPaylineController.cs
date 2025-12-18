using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FruitMaryPaylineController : MonoBehaviour
{
    #region Variables

    public static FruitMaryPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<FruitMaryPaylineData> paylines;

    // Currently running paylines
    public List<FruitMaryPaylineEntry> activePaylines = new List<FruitMaryPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2f;         
    private Coroutine animationLoop;
    public bool isShowing = false;                          

    [Header("Results")]
    [ShowInInspector][ReadOnly] public List<FruitMaryPaylineResult> spinResult = new List<FruitMaryPaylineResult>();
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


    public void AddPaylineData(FruitMaryPaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
            spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        spinResult.Clear();
    }

    public void StopPaylines()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
            {
                Image img = entry.payline.paylineSprite.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 0;   
                    img.color = c;
                }
            }
        }
        foreach (var reel in FruitMarySlotMachine.Instance.reels)
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

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<FruitMaryPaylineResult> results)
    {
        StopPaylines();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new FruitMaryPaylineEntry(paylineData, result.reelLimit));
            }
        }
        if (activePaylines.Count == 0 && resultScatterCount < 3)
        {
            Debug.LogWarning("No valid paylines to display.");
            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }
    public void ShowCollectedPaylines(int scatterCount)
    {
        resultScatterCount = scatterCount;
        if ((spinResult == null && spinResult.Count == 0 && resultScatterCount < 3))
        {
            FruitMarySlotMachine.Instance.isPaylineCompleted = true;
            return;
        }
        StartPaylineDisplay(spinResult); 
    }

    private IEnumerator PlayPaylines()
    {
        if (!FruitMaryAutoSpinController.isAutoSpinning && !FruitMarySlotMachine.Instance.isFreeGame)
        {
            //Debug.Log("LovKumar 1");
            FruitMarySlotMachine.Instance.isPaylineCompleted = true;
        }

        if (activePaylines.Count > 0)
        {
            foreach (var payline in activePaylines)
            {
                payline.payline.paylineSprite.SetActive(true);
            }

            yield return new WaitForSeconds(1f);
        }
        if (activePaylines.Count == 0)
        {
            //Debug.Log("LovKumar 2");
            FruitMarySlotMachine.Instance.isPaylineCompleted = true;
        }
        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                //Debug.Log("LovKumar 3");
                yield return PlaySinglePayline(activePaylines[0]);

                //Debug.Log("LovKumar 4");
                FruitMarySlotMachine.Instance.isPaylineCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    //Debug.Log("LovKumar 5");
                    foreach (var entry in activePaylines)
                    {
                        //Debug.Log("LovKumar 6");
                        yield return PlaySinglePayline(entry);
                    }
                    //Debug.Log("LovKumar 7");
                    FruitMarySlotMachine.Instance.isPaylineCompleted = true;

                    if ((FruitMarySlotMachine.Instance.isPaylineCompleted && FruitMaryAutoSpinController.isAutoSpinning) || FruitMarySlotMachine.Instance.isFreeGameReady || FruitMarySlotMachine.Instance.isFruitMaryGameReady)
                    {
                        isShowing = false;
                        break;
                    }
                   
                }
            }
            //Debug.Log("LovKumar 8");
            if (FruitMarySlotMachine.Instance.isFruitMaryGameReady)
            {
                //Debug.Log("LovKumar 9");
                yield return new WaitUntil(() => FruitMaryUIManager.Instance.winAnimationCompleted);
                //Debug.Log("LovKumar 10");
                FruitMaryUIManager.Instance.UpdateButtons("Transition Start");


                FruitMaryFruitMaryGameManager.Instance.StartFakeMaryGame(FruitMarySlotMachine.Instance.fruitMaryGameCount, 
                                                                           FruitMaryUIManager.Instance.CurrentBet(), FruitMarySlotMachine.Instance.GetWinAmount());
            }
            //Debug.Log("LovKumar 11");
        }
        if (resultScatterCount > 2)
        {
            yield return ScatterAnimation(resultScatterCount);
        }

    }
    private IEnumerator PlaySinglePayline(FruitMaryPaylineEntry entry)
    {
        foreach (var p in activePaylines)
            p.payline.paylineSprite.SetActive(false);

        if (entry.payline.paylineSprite != null)
        {
            entry.payline.paylineSprite.SetActive(true);
        }

        var img = entry.payline.paylineSprite.GetComponent<Image>();

        if (img != null)
        {
            var c = img.color;
            c.a = 1f;
            img.color = c;
        }

        float waitTime = flickerDelay;

        for (int x = 0; x < FruitMarySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitMarySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }
        if (FruitMarySlotMachine.Instance.isFreeGameReady || FruitMarySlotMachine.Instance.isFruitMaryGameReady)
        {
            yield return new WaitForSeconds(waitTime/2);
        }
        else
        {
            yield return new WaitForSeconds(waitTime);
        }

        if (activePaylines.Count > 1)
        {
            var reels = FruitMarySlotMachine.Instance.reels;
            foreach (var pl in activePaylines)
            {
                var mat = pl.payline.ToMatrix();

                for (int x = 0; x < reels.Count; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (mat[x, y] == 1 && x < pl.reelLimit)
                        {
                            if (y + 1 >= reels[x].slots.Count)
                                continue;

                            var slot = reels[x].slots[y + 1];
                            if (slot == null) continue;

                            slot.StopAnimation();
                        }
                    }
                }
            }

            if (entry.payline.paylineSprite != null)
                entry.payline.paylineSprite.SetActive(false);
        }
        
    }
    private IEnumerator ScatterAnimation(int scatterCount)
    {
        for (int x = 0; x < FruitMarySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitMarySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FruitMarySlotType.SCATTER)
                {
                    slot.PlayAnimation();
                }
            }
        }
        if (FruitMarySlotMachine.Instance.isFreeGame)
        {
            yield return new WaitForSeconds(2f);
        }
        yield return new WaitForSeconds(2f);
        if (FruitMarySlotMachine.Instance.freeSpinCount > 0 && !FruitMarySlotMachine.Instance.isFreeGame)
        {
            FruitMarySlotMachine.Instance.firstFreeSpin = true;
            FruitMaryUIManager.Instance.UpdateButtons("Transition Start");

            FruitMaryGameTransitionController.Instance.StartFreeSpinPopup();
            FruitMaryGameTransitionController.Instance.UpdateFreeSpinsCount(FruitMarySlotMachine.Instance.freeSpinCount);
        }
        //else if (FruitMarySlotMachine.Instance.freeSpinCount > 0)
        //{
        //    FruitMaryGameTransitionController.Instance.UpdateFreeSpinsCount(FruitMarySlotMachine.Instance.freeSpinCount);
        //}
        yield return new WaitForSeconds(2f);
        if (activePaylines.Count == 0)
        {
            FruitMarySlotMachine.Instance.isPaylineCompleted = true;
        }
        for (int x = 0; x < FruitMarySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitMarySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FruitMarySlotType.SCATTER)
                {
                    slot.StopAnimation();
                }
            }
        }
    }
    //private void OnDisable() => StopPaylines();
    //private void OnDestroy() => StopPaylines();
    #endregion
}