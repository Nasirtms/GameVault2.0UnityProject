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
    private Coroutine loopCo;
    public int FreeSpinsLeft { get; private set; }   
    public bool IsLastFreeSpin => FreeSpinsLeft == 0;
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
        if (loopCo != null) StopCoroutine(loopCo);
    }

    #endregion

    #region Public References
    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        totalFreeSpins = Mathf.Max(0, CrazySevenUIManager.Instance.freeGameSpinCount);
        if (totalFreeSpins <= 0) return;

        isFreeGame = true;
        freeSpinDone = 0;
        firstSpin = true;

        if (freeSpinCounterImage) freeSpinCounterImage.SetActive(true);
        InitialFreeSpinText();

        loopCo = StartCoroutine(FreeSpinFlow());
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

            UpdateSpinCount();

            float betAmount = CrazySevenUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isSpinAgain);

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
        if (loopCo != null) { StopCoroutine(loopCo); loopCo = null; }
    }
    #endregion
}