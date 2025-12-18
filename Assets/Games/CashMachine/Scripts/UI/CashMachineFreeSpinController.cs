using System.Collections;
using UnityEngine;

public class CashMachineFreeSpinController : MonoBehaviour
{
    public static CashMachineFreeSpinController Instance;

    [SerializeField] private float firstSpinDelay = 1.5f;
    [SerializeField] private float betweenSpinsDelay = 0.6f;

    private int freeSpinDone = 0;
    private int totalFreeSpins = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void StartFreeSpins()
    {
        if(CashMachineSlotMachine.Instance.isFreeGame) return;
        CashMachineSlotMachine.Instance.isFreeGame = true;
        CashMachineSlotMachine.Instance.isFreeGameReady = false;
        ResetFreeSpins();
        UpdateFreeSpins(1);
        StartCoroutine(FreeSpinLoop());
    }
    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(firstSpinDelay);
        //CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");

        while (freeSpinDone < totalFreeSpins)
        {
            yield return new WaitForSeconds(betweenSpinsDelay);
            float betAmount = CashMachineUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(()=> CashMachineSlotMachine.Instance.isSpinAgain);

            if(CashMachineSlotMachine.Instance.GetWinAmount() > 0)
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
        CashMachineSlotMachine.Instance.isFreeGame = false;
        CashMachineSlotMachine.Instance.decoyFreeSpinBool = false;
        CashMachineFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
}
