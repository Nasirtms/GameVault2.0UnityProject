using System.Collections;
using UnityEngine;

public class ComeOnCash2FreeSpinController : MonoBehaviour
{
    public static ComeOnCash2FreeSpinController Instance;

    [SerializeField] private float firstSpinDelay = 1.5f;
    [SerializeField] private float betweenSpinsDelay = 0.6f;

    private int freeSpinDone = 0;
    private int totalFreeSpins = 0;

    private Coroutine freeSpinRoutine;

    private bool cancelRequested;
    #region Unity Methods
    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
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

    public void StartFreeSpins()
    {
        if(ComeOnCash2SlotMachine.Instance.isFreeGame) return;

        cancelRequested = false;
        ComeOnCash2SlotMachine.Instance.isFreeGame = true;
        ComeOnCash2SlotMachine.Instance.isFreeGameReady = false;
        ResetFreeSpins();
        UpdateFreeSpins(1);
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }
    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(firstSpinDelay);
        //CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");

        while (freeSpinDone < totalFreeSpins)
        {
            yield return new WaitForSeconds(betweenSpinsDelay);

            if (cancelRequested) yield break;
            float betAmount = ComeOnCash2UIManager.Instance.CurrentBet();

            ComeOnCash2SlotMachine.Instance.lockedReels.Clear();

            if (!ComeOnCash2SlotMachine.Instance.canReel1spin)
            {
                ComeOnCash2SlotMachine.Instance.lockedReels.Add(1);
            }

            if (!ComeOnCash2SlotMachine.Instance.canReel2spin)
            {
                ComeOnCash2SlotMachine.Instance.lockedReels.Add(2);
            }

            if (!ComeOnCash2SlotMachine.Instance.canReel3spin)
            {
                ComeOnCash2SlotMachine.Instance.lockedReels.Add(3);
            }


            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => ComeOnCash2SlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (ComeOnCash2SlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ComeOnCash2SlotMachine.Instance.isPaylineCompleted);
            }

            freeSpinDone++;
        }

        yield return new WaitForSeconds(1.5f);
        EndFreeSpins();
    }

    public void ResetFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int spins)
    {
        totalFreeSpins += spins;
    }
    
    private void EndFreeSpins()
    {
        ResetFreeSpins();
        if(ComeOnCash2UIManager.Instance.reel2LockedBg.activeSelf) ComeOnCash2UIManager.Instance.reel2LockedBg.SetActive(false);
        if(ComeOnCash2UIManager.Instance.reel3LockedBg.activeSelf) ComeOnCash2UIManager.Instance.reel3LockedBg.SetActive(false);
        ComeOnCash2UIManager.Instance.betController.UpdateBetUi();
        ComeOnCash2SlotMachine.Instance.lockedReels.Clear();
        ComeOnCash2SlotMachine.Instance.isFreeGame = false;
        ComeOnCash2SlotMachine.Instance.decoyFreeSpinBool = false;
        ComeOnCash2FreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    private void CancelFreeSpins()
    {
        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
    }

}
