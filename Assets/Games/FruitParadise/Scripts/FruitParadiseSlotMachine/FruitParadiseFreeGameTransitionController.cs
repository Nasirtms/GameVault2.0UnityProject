using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FruitParadiseFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static FruitParadiseFreeGameTransitionController Instance;
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    [SerializeField] private GameObject freeSpinsCountText;
    [SerializeField] private GameObject jackpotText;

    private TMP_Text freeSpinWinText;

    private FruitParadiseFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<FruitParadiseFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        FruitParadiseSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }
    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitUntil(() => FruitParadiseSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => FruitParadiseUIManager.Instance.winAnimationCompleted);

        FruitParadisePaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(0f);

        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);
        ShowBackground();

        yield return new WaitForSeconds(0.5f);

        //freeSpinAnimator.SetTrigger("FreeSpin");

        yield return new WaitForSeconds(1.5f);

        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1.5f);

        jackpotText.SetActive(false);
        freeSpinsCountText.SetActive(true);

        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1f);

        FruitParadiseUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(0.5f);

        freeSpinsCountText.SetActive(false);
        jackpotText.SetActive(true);
        FruitParadisePaylineController.Instance.ClearPaylineData();
        FruitParadiseUIManager.Instance.UpdateButtons("Transition End");
        HideBackground();
        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);
        yield return new WaitForSeconds(1f);
        FruitParadiseUIManager.Instance.TextAnimation(FruitParadiseSlotMachine.Instance.freeSpinWinAmount, 3f, freeSpinWinText);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (FruitParadiseSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            FruitParadiseUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            WinAnimation();
        }
        FruitParadiseUIManager.Instance.spinButton.GetComponent<Button>().interactable = true;
    }

    private void WinAnimation()
    {
        if (FruitParadiseSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = FruitParadiseSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = FruitParadiseUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, FruitParadiseSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(FruitParadiseSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    } 
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);
        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }
    #endregion

    #region Helper Functions
    private void ShowBackground()
    {
        Sequence seq = DOTween.Sequence();
        normalBackgroundImage.gameObject.SetActive(true);
        freeSpinBackgroundImage.gameObject.SetActive(true);

        seq.Append(normalBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }

    private void HideBackground()
    {
        Sequence seq = DOTween.Sequence();
        normalBackgroundImage.gameObject.SetActive(true);
        freeSpinBackgroundImage.gameObject.SetActive(true);

        seq.Append(normalBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
    }
    #endregion
}
