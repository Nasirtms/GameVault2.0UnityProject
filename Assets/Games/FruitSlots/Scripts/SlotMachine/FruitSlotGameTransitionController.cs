using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class FruitSlotGameTransitionController : MonoBehaviour
{
    #region Variables

    public static FruitSlotGameTransitionController Instance;
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    [SerializeField] private GameObject freeSpinsCountText;

    private TMP_Text freeSpinWinText;

    private FruitSlotFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<FruitSlotFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        FruitSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitUntil(() => FruitSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => FruitSlotUIManager.Instance.winAnimationCompleted);

        FruitSlotPaylineController.Instance.ClearPaylineData();
        PopupAnimation(freeSpinStartFrame, 1f, 2f, true);
        ShowBackground();
        FruitSlotPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);

        //freeSpinAnimator.SetTrigger("FreeSpin");

        yield return new WaitForSeconds(1.5f);

        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1f);

        freeSpinsCountText.SetActive(true);

        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1f);

        FruitSlotUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(0.5f);

        freeSpinsCountText.SetActive(false);

        HideBackground();

        FruitSlotPaylineController.Instance.ClearPaylineData();

        PopupAnimation(freeSpinWinFrame, 1f, 2f, true);
        yield return new WaitForSeconds(1f);
        FruitSlotUIManager.Instance.TextAnimation(FruitSlotMachine.Instance.freeSpinWinAmount, 1.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (FruitSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            FruitSlotUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            WinAnimation();
        }
        FruitSlotUIManager.Instance.UpdateButtons("Transition End");
    }

    private void WinAnimation()
    {
        if (FruitSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = FruitSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = FruitSlotUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, FruitSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(FruitSlotMachine.Instance.UpdateGameCoin), 1f);
        }

        FruitSlotUIManager.Instance.spinButton.GetComponent<Button>().interactable = true;
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
