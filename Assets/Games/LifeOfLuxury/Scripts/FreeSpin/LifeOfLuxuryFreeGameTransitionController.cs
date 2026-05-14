using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeOfLuxuryFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static LifeOfLuxuryFreeGameTransitionController Instance;

    [SerializeField] private SpriteRenderer normalBackgroundImage;
    [SerializeField] private SpriteRenderer freeSpinBackgroundImage;
    [SerializeField] private SpriteRenderer normalMachineBgImage;
    [SerializeField] private SpriteRenderer freeSpinMachineBgImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    public GameObject freeSpinsCountText;
    public GameObject lineMultiplierObject;
    public TMP_Text freeSpinWinText;
    public TMP_Text freeSpinLineMultiplier;

    private LifeOfLuxuryFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<LifeOfLuxuryFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        LifeOfLuxurySlotMachine.Instance.isFreeGame = true;
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
    public void UpdateLineMultiplierText()
    {
        if (freeSpinLineMultiplier != null)
        {
            freeSpinLineMultiplier.text = LifeOfLuxurySlotMachine.Instance.freeSpinLineMultiplier.ToString();
        }
    }
    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitUntil(() => LifeOfLuxuryUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted);

        freeSpinLineMultiplier.text = $"{LifeOfLuxurySlotMachine.Instance.freeSpinLineMultiplier}";

        LifeOfLuxuryPaylineController.Instance.StopPaylines();
        LifeOfLuxuryPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);
        ShowBackground();
        //IrishPotLuckUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, true);
        lineMultiplierObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 0f, 0.5f, false);

        yield return new WaitForSeconds(1f);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        LifeOfLuxuryUIManager.Instance.UpdateButtons("Free Spin");
        //IrishPotLuckUIManager.Instance.StopMusic("Background");
        //IrishPotLuckUIManager.Instance.PlayMusic("FreeSpin_Background");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        //IrishPotLuckUIManager.Instance.StopMusic("FreeSpin_Background");
        //IrishPotLuckUIManager.Instance.PlayMusic("Background");

        yield return new WaitForSeconds(1.5f);
        LifeOfLuxuryPaylineController.Instance.StopPaylines();
        LifeOfLuxuryPaylineController.Instance.ClearPaylineData();

        yield return new WaitForSeconds(1f);
        HideBackground();
        LifeOfLuxuryUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        LifeOfLuxuryUIManager.Instance.TextAnimation(LifeOfLuxurySlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);
        lineMultiplierObject.SetActive(false);
        freeSpinsCountText.SetActive(false);
        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (LifeOfLuxurySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            LifeOfLuxuryUIManager.Instance.SetAutoInteractable(false);
            LifeOfLuxuryUIManager.Instance.SetSpinInteractable(false);
            WinAnimation();
        }
        else
        {
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Free Spin End");
        }
    }

    private void WinAnimation()
    {
        if (LifeOfLuxurySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = LifeOfLuxurySlotMachine.Instance.freeSpinWinAmount;
            float betAmount = LifeOfLuxuryUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, LifeOfLuxurySlotMachine.Instance.currentSpinResult.newBalance);
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Stop");
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
        seq.Append(normalMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }

    private void HideBackground()
    {
        Sequence seq = DOTween.Sequence();
        normalBackgroundImage.gameObject.SetActive(true);
        freeSpinBackgroundImage.gameObject.SetActive(true);

        seq.Append(normalBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Append(normalMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
    }
    #endregion
}