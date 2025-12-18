using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DoubleJackpotBullseyeFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static DoubleJackpotBullseyeFreeGameTransitionController Instance;

    [SerializeField] private Image freeSlotMechine;
    [SerializeField] private Image logo;
    [SerializeField] private Sprite normalLogo;
    [SerializeField] private Sprite respinLogo;
    [SerializeField] private float duration;
    [SerializeField] private Image hitMark;


    private TMP_Text freeSpinWinText;

    private DoubleJackpotBullseyeFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<DoubleJackpotBullseyeFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        DoubleJackpotBullseyeUIManager.Instance.StopMusic("Background");
        StartCoroutine(StartFreeSpin());
    }


    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }
    //public void NetworkErrorFreeSpin()
    //{
    //    freeSpinController.ErrorFreeSpinReturn();
    //}

    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        DoubleJackpotBullseyeUIManager.Instance.UpdateWinAmount(0);

        if (!DoubleJackpotBullseyeSlotMachine.Instance.playingwinanimation)
        {
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
            DoubleJackpotBullseyeSlotMachine.Instance.playingwinanimation = false;
        }

        DoubleJackpotBullseyeUIManager.Instance.PlaySound("FreeSpinPopup");
        StartCoroutine(BlinkCoroutine());
        Sequence seq = DOTween.Sequence();
        seq.Append(freeSlotMechine.DOFillAmount(1f, duration).SetEase(Ease.Linear));

        yield return new WaitForSeconds(duration);

        logo.sprite = respinLogo;

        yield return new WaitForSeconds(1f);

        DoubleJackpotBullseyeUIManager.Instance.PlayMusic("FreeSpinBackground");
        DoubleJackpotBullseyeFreeSpinController.Instance.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1.5f);
        DoubleJackpotBullseyeUIManager.Instance.StopMusic("FreeSpinBackground");

        Sequence seq = DOTween.Sequence();
        seq.Append(freeSlotMechine.DOFillAmount(0f, duration).SetEase(Ease.Linear));
        yield return new WaitForSeconds(duration);
        logo.sprite = normalLogo;

        yield return new WaitForSeconds(0.4f);

        DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame = false;
        
        yield return new WaitForSeconds(0.3f);

        if (DoubleJackpotBullseyeSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("exitfreeSpin");
        }
            
        DoubleJackpotBullseyeUIManager.Instance.PlayMusic("Background");
    }

    private void WinAnimation()
    {
        if (DoubleJackpotBullseyeSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = DoubleJackpotBullseyeSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = DoubleJackpotBullseyeUIManager.Instance.CurrentBet();
            DoubleJackpotBullseyeUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            if (freeGameWin >= (betAmount * 5000))
            {
                DoubleJackpotBullseyeUIManager.Instance.PlayJackpotWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 500))
            {
                DoubleJackpotBullseyeUIManager.Instance.PlaySuperWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 100))
            {
                DoubleJackpotBullseyeUIManager.Instance.PlayMegaWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 50))
            {
                DoubleJackpotBullseyeUIManager.Instance.PlayBigWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 10))
            {
                DoubleJackpotBullseyeUIManager.Instance.PlayNiceWinAnimation(freeGameWin);
            }
            else
            {
                DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
            }
        }
    }

    private IEnumerator BlinkCoroutine()
    {
        if (hitMark == null)
            yield break;

        Color baseColor = hitMark.color;
        float baseAlpha = baseColor.a;

        for (int i = 0; i < 3; i++)
        {
            // set alpha to 0 (invisible)
            hitMark.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            yield return new WaitForSeconds(0.3f);

            // set alpha to 1 (fully visible)
            hitMark.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            yield return new WaitForSeconds(0.3f);
        }

        // restore base alpha
        hitMark.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
    }

    #endregion
}
