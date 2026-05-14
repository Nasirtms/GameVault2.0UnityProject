using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldenDragonFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static GoldenDragonFreeGameTransitionController Instance;

    [SerializeField] private Image baseGameBG;
    [SerializeField] private Image freeSpinBG;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private TMP_Text freeSpinStartText;
    [SerializeField] private GameObject freeSpinsCountText;
    [SerializeField] private GameObject jackpot1Text;
    [SerializeField] private GameObject jackpot2Text;

    [SerializeField] private TMP_Text freeSpinWinText;
    private GoldenDragonFreeSpinController freeSpinController;
    public Animator freeSpinStartAnimator;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {

        freeSpinController = GetComponent<GoldenDragonFreeSpinController>();
    }

    #endregion

    #region Public References
    public void StartFreeSpinTransition()
    {
        GoldenDragonSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }
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
        yield return new WaitUntil(() => GoldenDragonSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => GoldenDragonUIManager.Instance.winAnimationCompleted);

        yield return new WaitForSeconds(1f);
        GoldenDragonPaylineController.Instance.ClearPaylineData();
        freeSpinWinText.text = "0.00";
        freeSpinStartText.text = $"You have got {freeSpinController.totalFreeSpins} free spins";
        freeSpinStartFrame.SetActive(true);
        freeSpinStartAnimator = freeSpinStartFrame.transform.GetChild(0).GetComponent<Animator>();
        freeSpinStartAnimator.SetBool("Start", true);
        ShowBackground();

        yield return new WaitForSeconds(2.8f);

        freeSpinStartFrame.SetActive(false);
        freeSpinStartAnimator.SetBool("Start", false);

        yield return new WaitForSeconds(1.5f);

        jackpot1Text.SetActive(false);
        jackpot2Text.SetActive(false);
        freeSpinsCountText.SetActive(true);

        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1f);

        GoldenDragonUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
        GoldenDragonUIManager.Instance.StopMusic("BG");
        GoldenDragonUIManager.Instance.PlayMusic("FreeSpinBG");
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1f);

        freeSpinsCountText.SetActive(false);
        jackpot1Text.SetActive(true);
        jackpot2Text.SetActive(true);

        GoldenDragonPaylineController.Instance.ClearPaylineData();
        GoldenDragonUIManager.Instance.UpdateButtons("Transition End");
        GoldenDragonUIManager.Instance.StopMusic("FreeSpinBG");
        GoldenDragonUIManager.Instance.PlayMusic("BG");
        HideBackground();

        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);
        yield return new WaitForSeconds(1f);

        GoldenDragonUIManager.Instance.TextAnimation(GoldenDragonSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (GoldenDragonSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            GoldenDragonUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;
            GoldenDragonSlotMachine.Instance.isFreeGameEnded = true;
            GoldenDragonSlotMachine.Instance.ShowMiniGameButton();
            WinAnimation();
        }
        GoldenDragonUIManager.Instance.spinButton.GetComponent<Button>().interactable = true;
    }

    private void WinAnimation()
    {
        if (GoldenDragonSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = GoldenDragonSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = GoldenDragonUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, GoldenDragonSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(GoldenDragonSlotMachine.Instance.UpdateGameCoin), 1f);
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
        baseGameBG.gameObject.SetActive(true);
        freeSpinBG.gameObject.SetActive(true);

        seq.Append(baseGameBG.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBG.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }

    private void HideBackground()
    {
        Sequence seq = DOTween.Sequence();
        baseGameBG.gameObject.SetActive(true);
        freeSpinBG.gameObject.SetActive(true);

        seq.Append(baseGameBG.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBG.DOFade(0f, 1f).SetEase(Ease.InOutSine));
    }
    #endregion
}