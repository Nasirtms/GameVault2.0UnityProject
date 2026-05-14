using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldenWheelFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static GoldenWheelFreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpin;
    [SerializeField] private Image freeSpinBg;
    [SerializeField] private GameObject wheel;
    [SerializeField] private Image BonusGameBG;
    [SerializeField] public Button StartButton;

    [SerializeField] private float duration;


    private TMP_Text freeSpinWinText;

    private GoldenWheelFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<GoldenWheelFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        GoldenWheelUIManager.Instance.StopMusic("Background");
        StartCoroutine(StartFreeSpin());
    }

    public void StartBonusGameTransition(int index)
    {
        GoldenWheelUIManager.Instance.StopMusic("Background");
        StartCoroutine(StartBonusGame(index));
    }

    public void EndBonusGameTransition()
    {
        StartCoroutine(EndBonusGame());
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
        if(GoldenWheelSlotMachine.Instance.GetWinAmount() > 0)
        {
            yield return new WaitUntil(() => GoldenWheelUIManager.Instance.winAnimationCompleted);
        }
        yield return new WaitUntil(() => GoldenWheelSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitForSeconds(0.8f);

        freeSpin.SetActive(true);
        freeSpin.GetComponent<Animator>().SetBool("start", true);

        yield return new WaitForSeconds(0.4f);
        
        freeSpin.GetComponent<Animator>().SetBool("start", false);
        yield return new WaitForSeconds(0.6f);
        freeSpin.SetActive(false);

        BlinkBackground();

        GoldenWheelFreeSpinController.Instance.StartFreeSpins();
    }

    private IEnumerator StartBonusGame(int index)
    {
        if (GoldenWheelSlotMachine.Instance.GetWinAmount() > 0)
        {
            yield return new WaitUntil(() => GoldenWheelUIManager.Instance.winAnimationCompleted);
        }
        yield return new WaitUntil(() => GoldenWheelSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitForSeconds(0.8f);

        StartButton.interactable = false;

        //BonusGameBG.transform.GetComponent<Image>().DOFade(0.5f, 1.5f).SetEase(Ease.InOutSine);
        //wheel.transform.GetComponent<Animator>().SetBool("out", true);
        wheel.transform.GetComponent<Animator>().SetBool("in", true);
        yield return new WaitForSeconds(1f);
        wheel.transform.GetComponent<Animator>().SetBool("in", false);

        StartButton.interactable = true;

        GoldenWheelFreeSpinController.Instance.StartBonusGame(index);
    }

    private IEnumerator EndBonusGame()
    {
        wheel.transform.GetComponent<Animator>().SetBool("out", true);
        yield return new WaitForSeconds(1f);
        wheel.transform.GetComponent<Animator>().SetBool("out", false);
        StartButton.gameObject.SetActive(true);
        GoldenWheelSlotMachine.Instance.isBonusGame = false;
        GoldenWheelSlotMachine.Instance.isBonusGameReady = false;
    }

    private IEnumerator EndFreeSpin()
    {
        freeSpinBg.DOKill();
        yield return new WaitForSeconds(0.1f);
        freeSpinBg.gameObject.SetActive(false);

        GoldenWheelSlotMachine.Instance.isFreeGame = false;

        if (GoldenWheelSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            GoldenWheelUIManager.Instance.UpdateButtons("exitfreeSpin");
        }

        GoldenWheelUIManager.Instance.PlayMusic("Background");
    }

    private void WinAnimation()
    {
        if (GoldenWheelSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = GoldenWheelSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = GoldenWheelUIManager.Instance.CurrentBet();
            GoldenWheelUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            if (freeGameWin >= (betAmount * 5000))
            {
                GoldenWheelUIManager.Instance.PlayJackpotWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 500))
            {
                GoldenWheelUIManager.Instance.PlaySuperWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 100))
            {
                GoldenWheelUIManager.Instance.PlayMegaWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 50))
            {
                GoldenWheelUIManager.Instance.PlayBigWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 10))
            {
                GoldenWheelUIManager.Instance.PlayNiceWinAnimation(freeGameWin);
            }
            else
            {
                GoldenWheelUIManager.Instance.UpdateButtons("Stop");
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
