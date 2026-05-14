using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RedHotTrippleFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static RedHotTrippleFreeGameTransitionController Instance;
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] public GameObject freeSpinsCountText;
    [SerializeField] public TMP_Text freeSpinWinText;
    [SerializeField] public TMP_Text totalFreeSpins;
    public GameObject topImage;
    private RedHotTrippleFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<RedHotTrippleFreeSpinController>();
    }

    #endregion

    #region Public References
    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        RedHotTrippleSlotMachine.Instance.isFreeGame = true;
        RedHotTrippleSlotMachine.Instance.UpdateCachedSymbols();
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
        yield return new WaitUntil(() => RedHotTrippleUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => RedHotTrippleSlotMachine.Instance.isPaylineCompleted);
        RedHotTripplePaylineController.Instance.StopPaylineLoop();
        RedHotTripplePaylineController.Instance.ClearPaylineResults();
        yield return new WaitForSeconds(1f);
        totalFreeSpins.text = $"{RedHotTrippleSlotMachine.Instance.freeSpinCount}";
        ShowBackground();
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, true);
        //RedHotTrippleUIManager.Instance.StopMusic("BG");
        //RedHotTrippleUIManager.Instance.PlaySound("FreeSpinStart");
        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(0.5f);
        topImage.SetActive(false);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        RedHotTrippleUIManager.Instance.UpdateButtons("FreeSpin");
        //RedHotTrippleUIManager.Instance.PlayMusic("FreeSpin");
        yield return new WaitForSeconds(1.5f);

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        RedHotTripplePaylineController.Instance.StopPaylineLoop();
        RedHotTripplePaylineController.Instance.ClearPaylineResults();

        yield return new WaitForSeconds(0.5f);

        //RedHotTrippleUIManager.Instance.StopMusic("FreeSpin");
        //RedHotTrippleUIManager.Instance.PlayMusic("BG");
        yield return new WaitForSeconds(0.5f);
        HideBackground();

        freeSpinsCountText.SetActive(false);
        topImage.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);

        RedHotTrippleUIManager.Instance.TextAnimation(RedHotTrippleSlotMachine.Instance.freeSpinWinAmount, 3.5f, freeSpinWinText);

        yield return new WaitForSeconds(4.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";
        if (RedHotTrippleSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("Stop");
        }
    }

    private void WinAnimation()
    {
        if (RedHotTrippleSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = RedHotTrippleSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = RedHotTrippleUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, RedHotTrippleSlotMachine.Instance.currentSpinResult.newBalance);
            RedHotTrippleUIManager.Instance.UpdateButtons("Stop");
        }
    }
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }
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