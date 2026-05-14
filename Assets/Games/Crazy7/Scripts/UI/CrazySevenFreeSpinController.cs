using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
[RequireComponent(typeof(CrazySevenGameTransitionController))]
public class CrazySevenFreeSpinController : MonoBehaviour
{
    #region Variables

    [Header("UI")]
    [SerializeField] private GameObject freeSpinCounterImage;
    [SerializeField]  private TMP_Text freeSpinsText;

    [Header("Timings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;
    private CrazySevenGameTransitionController popup;              

    public int FreeSpinsLeft;
    public bool IsLastFreeSpin => FreeSpinsLeft == 0;

    private Coroutine freeSpinRoutine;
    private bool cancelRequested;
    #endregion

    #region Unity Methods

    private void Start()
    {
        popup = GetComponent<CrazySevenGameTransitionController>();
        if (freeSpinCounterImage)
            freeSpinsText = freeSpinCounterImage.GetComponentInChildren<TMP_Text>(true);

        if (freeSpinCounterImage) freeSpinCounterImage.SetActive(false);
    }

    private void OnDestroy()
    {
        if (freeSpinRoutine != null) StopCoroutine(freeSpinRoutine);
    }
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

        totalFreeSpins = Mathf.Max(0, CrazySevenUIManager.Instance.freeGameSpinCount);
        if (totalFreeSpins <= 0) return;
        cancelRequested = false;
        isFreeGame = true;
        freeSpinDone = 0;
        firstSpin = true;

        if (freeSpinCounterImage) freeSpinCounterImage.SetActive(true);
        InitialFreeSpinText();

        freeSpinRoutine = StartCoroutine(FreeSpinFlow());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
        freeSpinDone = 0;
    }

    public void UpdateFreeSpins(int newTotal)
    {
        totalFreeSpins += Mathf.Max(newTotal, 0);
        UpdateSpinCount();
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    public void InitialFreeSpinText()
    {
        if (!freeSpinsText) return;
        freeSpinsText.text = totalFreeSpins > 0 ? $"1/{totalFreeSpins}" : string.Empty;
    }

    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (!freeSpinsText || totalFreeSpins <= 0) return;
        int current = Mathf.Clamp(freeSpinDone + 1, 1, totalFreeSpins);
        freeSpinsText.text = $"{current}/{totalFreeSpins}";
    }

    private IEnumerator FreeSpinFlow()
    {

        yield return new WaitForSeconds(0.25f);
        InitialFreeSpinText();

        while (true)
        {
            if (freeSpinDone >= totalFreeSpins)
                break;  // Only exit if no more spins left

            CrazySevenUIManager.Instance.UpdateButtons("Free Spin");

            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            if (cancelRequested) yield break;
            UpdateSpinCount();

            float betAmount = CrazySevenUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isSpinAgain);
            if (cancelRequested) yield break;
            if (CrazySevenSlotMachine.Instance.currentSpinResult != null)
            {
                if (CrazySevenSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isPaylineCompleted);
                }
            }
            freeSpinDone++;
        }
        yield return new WaitForSeconds(1f);
        EndFreeSpins();
    }

    public void EndFreeSpins()
    {
        isFreeGame = false;

        CrazySevenPaylineController.Instance.StopPaylines();
        CrazySevenSlotMachine.Instance.isFreeGame = false;

        float totalWin = CrazySevenUIManager.Instance.freeGameWinAmount;
        popup.EndFreeSpin();

        CrazySevenUIManager.Instance.freeGameSpinCount = 0;
        if (freeSpinCounterImage) freeSpinCounterImage.SetActive(false);
        if (freeSpinRoutine != null) { StopCoroutine(freeSpinRoutine); freeSpinRoutine = null; }
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
        CrazySevenUIManager.Instance.UpdateButtons("FreeSpin Stop");
    }

    #endregion
}