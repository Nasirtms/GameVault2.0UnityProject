using System.Collections;
using UnityEngine;

public class GoldenWheelFreeSpinController : MonoBehaviour
{
    public static GoldenWheelFreeSpinController Instance;

    [SerializeField] private float firstSpinDelay = 1.5f;
    [SerializeField] private float betweenSpinsDelay = 0.6f;
    [SerializeField] private Transform[] segments;
    [SerializeField] private Transform wheelRoot;   // parent transform of all segments
    [SerializeField] private float spinDuration = 3.5f;
    [SerializeField] private int extraFullSpins = 4; // how many full rotations before stopping

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
        if(GoldenWheelSlotMachine.Instance.isFreeGame) return;
        cancelRequested = false;
        GoldenWheelSlotMachine.Instance.isFreeGame = true;
        GoldenWheelSlotMachine.Instance.isFreeGameReady = false;
        ResetFreeSpins();
        UpdateFreeSpins(1);
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void StartBonusGame(int index)
    {
        if(GoldenWheelSlotMachine.Instance.isBonusGame) return;
        GoldenWheelSlotMachine.Instance.isBonusGame = true;
        GoldenWheelSlotMachine.Instance.isBonusGameReady = false;

        GoldenWheelFreeGameTransitionController.Instance.StartButton.onClick.RemoveAllListeners();
        GoldenWheelFreeGameTransitionController.Instance.StartButton.onClick.AddListener(() => StartBonusButtonListner(index));
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(firstSpinDelay);
        //CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");

        while (freeSpinDone < totalFreeSpins)
        {
            yield return new WaitForSeconds(betweenSpinsDelay);
            if (cancelRequested) yield break;
            float betAmount = GoldenWheelUIManager.Instance.CurrentBet();

            if (GoldenWheelSlotMachine.Instance.canReel2spin && !GoldenWheelSlotMachine.Instance.canReel3spin)
            {
                GoldenWheelSlotMachine.Instance.LockedReels.Clear();
                GoldenWheelSlotMachine.Instance.LockedReels.Add(3);
            }
            else if (!GoldenWheelSlotMachine.Instance.canReel2spin && GoldenWheelSlotMachine.Instance.canReel3spin)
            {
                GoldenWheelSlotMachine.Instance.LockedReels.Clear();
                GoldenWheelSlotMachine.Instance.LockedReels.Add(2);
            }
            else if (!GoldenWheelSlotMachine.Instance.canReel2spin && !GoldenWheelSlotMachine.Instance.canReel3spin)
            {
                GoldenWheelSlotMachine.Instance.LockedReels.Clear();
                GoldenWheelSlotMachine.Instance.LockedReels.Add(2);
                GoldenWheelSlotMachine.Instance.LockedReels.Add(3);
            }
            else if (GoldenWheelSlotMachine.Instance.canReel2spin && GoldenWheelSlotMachine.Instance.canReel3spin)
            {
                GoldenWheelSlotMachine.Instance.LockedReels.Clear();
            }

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(()=> GoldenWheelSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (GoldenWheelSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => GoldenWheelSlotMachine.Instance.isPaylineCompleted);
            }

            freeSpinDone++;
        }

        yield return new WaitForSeconds(1.5f);
        EndFreeSpins();
    }

    private void StartBonusButtonListner(int index)
    {
        GoldenWheelFreeGameTransitionController.Instance.StartButton.gameObject.SetActive(false);
        StartCoroutine(BonusWheel(index));
    }

    private IEnumerator BonusWheel(int index)
    {
        GoldenWheelSlotMachine.Instance.isBonusGameReady = false;

        int segmentCount = 9;
        float anglePerSegment = 360f / segmentCount;

        // Normalize index
        index = Mathf.Clamp(index, 0, segmentCount - 1);

        // Current rotation
        float startAngle = wheelRoot.eulerAngles.z;

        // Final angle so selected segment lands on TOP
        float targetAngle = ((index * anglePerSegment) + 2);

        // Add extra spins
        float totalRotation = (extraFullSpins * 360f) + Mathf.DeltaAngle(startAngle, targetAngle);
        float endAngle = startAngle + totalRotation;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinDuration;

            // Ease-out curve (manual, smooth & controllable)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            float currentAngle = Mathf.Lerp(startAngle, endAngle, easedT);
            wheelRoot.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }

        wheelRoot.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        yield return new WaitForSeconds(1.5f);
        GoldenWheelFreeGameTransitionController.Instance.EndBonusGameTransition();
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
        if(GoldenWheelUIManager.Instance.reel2LockedBg.activeSelf) GoldenWheelUIManager.Instance.reel2LockedBg.SetActive(false);
        if(GoldenWheelUIManager.Instance.reel3LockedBg.activeSelf) GoldenWheelUIManager.Instance.reel3LockedBg.SetActive(false);
        GoldenWheelUIManager.Instance.betController.UpdateBetUi();
        GoldenWheelSlotMachine.Instance.LockedReels.Clear();
        GoldenWheelSlotMachine.Instance.freeSpinsDone = 0;
        GoldenWheelSlotMachine.Instance.isFreeGame = false;
        GoldenWheelSlotMachine.Instance.decoyFreeSpinBool = false;
        GoldenWheelFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }

    private void CancelFreeSpins()
    {
        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        GoldenWheelUIManager.Instance.UpdateButtons("Stop");
    }
}
