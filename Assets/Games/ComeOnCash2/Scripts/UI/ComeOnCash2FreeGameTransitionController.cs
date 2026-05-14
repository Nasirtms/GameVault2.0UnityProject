using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComeOnCash2FreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static ComeOnCash2FreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpin;
    [SerializeField] private Image freeSpinBg;

    [SerializeField] private float duration;


    private TMP_Text freeSpinWinText;

    private ComeOnCash2FreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<ComeOnCash2FreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        ComeOnCash2UIManager.Instance.StopMusic("Background");
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
        if (ComeOnCash2SlotMachine.Instance.GetWinAmount() > 0)
        {
            yield return new WaitUntil(() => ComeOnCash2UIManager.Instance.winAnimationCompleted);
        }
        yield return new WaitUntil(() => ComeOnCash2SlotMachine.Instance.isPaylineCompleted);
        yield return new WaitForSeconds(0.8f);

        freeSpin.SetActive(true);
        freeSpin.GetComponent<Animator>().SetBool("start", true);

        yield return new WaitForSeconds(0.4f);
        
        freeSpin.GetComponent<Animator>().SetBool("start", false);
        yield return new WaitForSeconds(0.6f);
        freeSpin.SetActive(false);

        BlinkBackground();

        ComeOnCash2FreeSpinController.Instance.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        freeSpinBg.DOKill();
        yield return new WaitForSeconds(0.1f);
        freeSpinBg.gameObject.SetActive(false);

        ComeOnCash2SlotMachine.Instance.isFreeGame = false;

        if (ComeOnCash2SlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            ComeOnCash2UIManager.Instance.UpdateButtons("exitfreeSpin");
        }

        ComeOnCash2UIManager.Instance.PlayMusic("Background");
    }

    private void WinAnimation()
    {
        if (ComeOnCash2SlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = ComeOnCash2SlotMachine.Instance.freeSpinWinAmount;
            float betAmount = ComeOnCash2UIManager.Instance.CurrentBet();
            ComeOnCash2UIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            if (freeGameWin >= (betAmount * 5000))
            {
                ComeOnCash2UIManager.Instance.PlayJackpotWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 500))
            {
                ComeOnCash2UIManager.Instance.PlaySuperWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 100))
            {
                ComeOnCash2UIManager.Instance.PlayMegaWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 50))
            {
                ComeOnCash2UIManager.Instance.PlayBigWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 10))
            {
                ComeOnCash2UIManager.Instance.PlayNiceWinAnimation(freeGameWin);
            }
            else
            {
                ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
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
