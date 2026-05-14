using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
[RequireComponent(typeof(FruitMaryGameTransitionController))]
public class FruitMaryFreeSpinController : MonoBehaviour
{
    #region Variables

    [Header("UI")]
    [SerializeField] public GameObject freeSpinCounterImage;
    [SerializeField] private TMP_Text freeSpinsText;

    [Header("Timings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;

    private Coroutine freeSpinRoutine;
    private bool cancelRequested;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += CancelFreeSpins;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= CancelFreeSpins;
    }
    #endregion

    #region Public References
    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        cancelRequested = false;
        isFreeGame = true;
        freeSpinDone = 0;
        firstSpin = true;

        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins += freeSpins;
        UpdateSpinCount();
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"0/{totalFreeSpins}";
    }

    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{freeSpinDone}/{totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(1f);
        while (freeSpinDone < totalFreeSpins)
        {
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins); 
            }
            if (cancelRequested) yield break;

            float betAmount = FruitMaryUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);
            freeSpinDone++;
            UpdateSpinCount();

            yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (FruitMarySlotMachine.Instance.currentSpinResult != null)
            {
                if (FruitMarySlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isPaylineCompleted);
                    yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isBonusGameCompleted);
                }
            }

            if (FruitMarySlotMachine.Instance.freeSpinCount > 0)
            {
                totalFreeSpins += FruitMarySlotMachine.Instance.freeSpinCount;
                UpdateSpinCount();
            }
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
        EndFreeSpins();
    }

    public void EndFreeSpins()
    {
        isFreeGame = false;
        freeSpinDone = 0;
        totalFreeSpins = 0;
        FruitMaryPaylineController.Instance.StopPaylines();
        FruitMarySlotMachine.Instance.isFreeGame = false;
        FruitMaryUIManager.Instance.freeGameSpinCount = 0;

        FruitMaryGameTransitionController.Instance.EndFreeSpinTransition();
    }
    private void CancelFreeSpins()
    {
        if (!isFreeGame) return;

        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        FruitMaryUIManager.Instance.UpdateButtons("FreeSpin End");
    }
    #endregion
}