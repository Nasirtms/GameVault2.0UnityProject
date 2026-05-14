using System.Collections;
using UnityEngine;

public class CashMachineFreeSpinController : MonoBehaviour
{
    public static CashMachineFreeSpinController Instance;

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
        if(CashMachineSlotMachine.Instance.isFreeGame) return;
        cancelRequested = false;
        CashMachineSlotMachine.Instance.isFreeGame = true;
        CashMachineSlotMachine.Instance.isFreeGameReady = false;
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
            float betAmount = CashMachineUIManager.Instance.CurrentBet();

            if (CashMachineSlotMachine.Instance.canReel2spin && !CashMachineSlotMachine.Instance.canReel3spin)
            {
                CashMachineSlotMachine.Instance.LockedReels.Clear();
                CashMachineSlotMachine.Instance.LockedReels.Add(3);
            }
            else if (!CashMachineSlotMachine.Instance.canReel2spin && CashMachineSlotMachine.Instance.canReel3spin)
            {
                CashMachineSlotMachine.Instance.LockedReels.Clear();
                CashMachineSlotMachine.Instance.LockedReels.Add(2);
            }
            else if (!CashMachineSlotMachine.Instance.canReel2spin && !CashMachineSlotMachine.Instance.canReel3spin)
            {
                CashMachineSlotMachine.Instance.LockedReels.Clear();
                CashMachineSlotMachine.Instance.LockedReels.Add(2);
                CashMachineSlotMachine.Instance.LockedReels.Add(3);
            }
            else if (CashMachineSlotMachine.Instance.canReel2spin && CashMachineSlotMachine.Instance.canReel3spin)
            {
                CashMachineSlotMachine.Instance.LockedReels.Clear();
            }

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(()=> CashMachineSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (CashMachineSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => CashMachineSlotMachine.Instance.isPaylineCompleted);
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
        if(CashMachineUIManager.Instance.reel2LockedBg.activeSelf) CashMachineUIManager.Instance.reel2LockedBg.SetActive(false);
        if(CashMachineUIManager.Instance.reel3LockedBg.activeSelf) CashMachineUIManager.Instance.reel3LockedBg.SetActive(false);
        CashMachineUIManager.Instance.betController.UpdateBetUi();
        CashMachineSlotMachine.Instance.LockedReels.Clear();
        CashMachineSlotMachine.Instance.freeSpinsDone = 0;
        CashMachineSlotMachine.Instance.isFreeGame = false;
        CashMachineSlotMachine.Instance.decoyFreeSpinBool = false;
        CashMachineFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }

    private void CancelFreeSpins()
    {
        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        CashMachineUIManager.Instance.UpdateButtons("Stop");
    }
}
