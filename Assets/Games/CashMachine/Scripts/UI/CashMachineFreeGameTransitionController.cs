using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CashMachineFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static CashMachineFreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpin;
    [SerializeField] private Image freeSpinBg;

    [SerializeField] private float duration;


    private TMP_Text freeSpinWinText;

    private CashMachineFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<CashMachineFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        CashMachineUIManager.Instance.StopMusic("Background");
        StartCoroutine(StartFreeSpin());
    }


    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitForSeconds(0.8f);

        freeSpin.SetActive(true);
        freeSpin.GetComponent<Animator>().SetBool("start", true);

        yield return new WaitForSeconds(0.4f);
        
        freeSpin.GetComponent<Animator>().SetBool("start", false);
        yield return new WaitForSeconds(0.6f);
        freeSpin.SetActive(false);

        BlinkBackground();

        CashMachineFreeSpinController.Instance.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        freeSpinBg.DOKill();
        yield return new WaitForSeconds(0.1f);
        freeSpinBg.gameObject.SetActive(false);

        CashMachineSlotMachine.Instance.isFreeGame = false;

        if (CashMachineSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            CashMachineUIManager.Instance.UpdateButtons("exitfreeSpin");
        }

        CashMachineUIManager.Instance.PlayMusic("Background");
    }

    private void WinAnimation()
    {
        if (CashMachineSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = CashMachineSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = CashMachineUIManager.Instance.CurrentBet();
            CashMachineUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            if (freeGameWin >= (betAmount * 5000))
            {
                CashMachineUIManager.Instance.PlayJackpotWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 500))
            {
                CashMachineUIManager.Instance.PlaySuperWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 100))
            {
                CashMachineUIManager.Instance.PlayMegaWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 50))
            {
                CashMachineUIManager.Instance.PlayBigWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 10))
            {
                CashMachineUIManager.Instance.PlayNiceWinAnimation(freeGameWin);
            }
            else
            {
                CashMachineUIManager.Instance.UpdateButtons("Stop");
            }
        }
    }


    private void BlinkBackground()
    {
        freeSpinBg.gameObject.SetActive(true);

        Color c = freeSpinBg.color;
        c.a = 0.7f;
        freeSpinBg.color = c;

        freeSpinBg
            .DOFade(1f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    #endregion
}
