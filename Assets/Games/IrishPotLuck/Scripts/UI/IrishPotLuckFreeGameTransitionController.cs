using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IrishPotLuckFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static IrishPotLuckFreeGameTransitionController Instance;

    [SerializeField] private SpriteRenderer normalBackgroundImage;
    [SerializeField] private SpriteRenderer freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    public GameObject freeSpinsCountText;

    public TMP_Text freeSpinWinText;

    private IrishPotLuckFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<IrishPotLuckFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        IrishPotLuckSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitUntil(() => IrishPotLuckUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted);

        IrishPotLuckPaylineController.Instance.StopPaylines();
        IrishPotLuckPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);
        ShowBackground();
        //IrishPotLuckUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(freeSpinStartFrame, 2f, 0.5f, true);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 0f, 0.5f, false);

        yield return new WaitForSeconds(1f);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        IrishPotLuckUIManager.Instance.UpdateButtons("Free Spin");
        //IrishPotLuckUIManager.Instance.StopMusic("Background");
        //IrishPotLuckUIManager.Instance.PlayMusic("FreeSpin_Background");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        //IrishPotLuckUIManager.Instance.StopMusic("FreeSpin_Background");
        //IrishPotLuckUIManager.Instance.PlayMusic("Background");

        yield return new WaitForSeconds(1.5f);
        IrishPotLuckPaylineController.Instance.StopPaylines();
        IrishPotLuckPaylineController.Instance.ClearPaylineData();

        yield return new WaitForSeconds(0.5f);
        HideBackground();
        //gameTitle.SetActive(true);
        freeSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        IrishPotLuckUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 2f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        IrishPotLuckUIManager.Instance.TextAnimation(IrishPotLuckSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (IrishPotLuckSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            IrishPotLuckUIManager.Instance.SetAutoInteractable(false);
            IrishPotLuckUIManager.Instance.SetSpinInteractable(false);
            WinAnimation();
        }
        else
        {
            IrishPotLuckUIManager.Instance.UpdateButtons("Free Spin End");
        }
    }

    private void WinAnimation()
    {
        if (IrishPotLuckSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = IrishPotLuckSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = IrishPotLuckUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, IrishPotLuckSlotMachine.Instance.currentSpinResult.newBalance);
            IrishPotLuckUIManager.Instance.UpdateButtons("Stop");
        }
    }
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

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