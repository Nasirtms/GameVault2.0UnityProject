using System.Collections;
using UnityEngine;

public class DoubleJackpotBullseyeFreeSpinController : MonoBehaviour
{
    public static DoubleJackpotBullseyeFreeSpinController Instance;

    [SerializeField] private float firstSpinDelay = 1.5f;
    [SerializeField] private float betweenSpinsDelay = 0.6f;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void StartFreeSpins()
    {
        StopAllCoroutines();
        StartCoroutine(FreeSpinLoop());
    }
    private IEnumerator FreeSpinLoop()
    {
        var sm = DoubleJackpotBullseyeSlotMachine.Instance;

        // safety: mark free-game on, UI state as in-spin
        sm.InSpin = false;
        sm.isSpinAgain = false;
        sm.isPaylineCompleted = false;
        sm.freeSpinWinAmount = 0f;

        yield return new WaitForSeconds(firstSpinDelay);
        DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("enterfreeSpin");

        while (true)
        {
            // 1) spin (only reels 0 & 2 will move; reel 1 is frozen by SlotMachine logic below)
            float bet = DoubleJackpotBullseyeUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(bet);

            // 2) wait until reels stop (slot machine sets isSpinAgain)
            yield return new WaitUntil(() => sm.isSpinAgain);

            // 3) check if this spin has at least one payline win
            var r = sm.currentSpinResult;
            bool hasWin = r != null && r.paylineWins != null && r.paylineWins.Count > 0;

            // 4) if win → show paylines and wait until finished

            if (sm.errorFreeSpin)
            {
                sm.errorFreeSpin = false;
                //yield return null;
                sm.isPaylineCompleted = true;
                yield return new WaitUntil(() => sm.isPaylineCompleted);
            }
            else if (hasWin)
            {
                yield return new WaitUntil(() => sm.isPaylineCompleted);
            }
            else
            {
                // first miss ends the feature
                break;
            }

            yield return new WaitForSeconds(betweenSpinsDelay);

            // reset per-spin gates before next loop
            sm.isSpinAgain = false;
            sm.isPaylineCompleted = false;
        }

        // end transition
        DoubleJackpotBullseyeFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
}
