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
    [SerializeField] private SpriteRenderer normalBackgroundImage1;
    [SerializeField] private SpriteRenderer freeSpinBackgroundImage1;
    [SerializeField] private SpriteRenderer normalMachineBgImage;
    [SerializeField] private SpriteRenderer freeSpinMachineBgImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;

    public GameObject freeSpinsCountText;
    public GameObject lineMultiplierObject;
    public TMP_Text freeSpinWinText;
    public TMP_Text freeSpinLineMultiplier;

    public GameObject lineMultiplierTextsObject;
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

        SetAlpha(normalBackgroundImage, 1f);
        SetAlpha(normalBackgroundImage1, 1f);
        SetAlpha(normalMachineBgImage, 1f);

        SetAlpha(freeSpinBackgroundImage, 0f);
        SetAlpha(freeSpinBackgroundImage1, 0f);
        SetAlpha(freeSpinMachineBgImage, 0f);

        normalBackgroundImage.gameObject.SetActive(true);
        normalBackgroundImage1.gameObject.SetActive(true);
        normalMachineBgImage.gameObject.SetActive(true);

        freeSpinBackgroundImage.gameObject.SetActive(false);
        freeSpinBackgroundImage1.gameObject.SetActive(false);
        freeSpinMachineBgImage.gameObject.SetActive(false);
    }

    private void SetAlpha(SpriteRenderer sprite, float alpha)
    {
        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
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

        yield return new WaitForSeconds(1.5f);
        LifeOfLuxuryUIManager.Instance.StopMusic("BG");
        ShowBackground();
        LifeOfLuxuryUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);

        lineMultiplierObject.SetActive(true);
        lineMultiplierTextsObject.SetActive(false);
        yield return new WaitForSeconds(2.5f);

        LifeOfLuxuryPaylineController.Instance.StopPaylines();
        LifeOfLuxuryPaylineController.Instance.ClearPaylineData();

        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        PopupAnimation(freeSpinStartFrame, 0f, 1f, false);

        yield return new WaitForSeconds(1f);
        LifeOfLuxuryUIManager.Instance.UpdateButtons("Free Spin");

        LifeOfLuxuryUIManager.Instance.PlayMusic("FreeSpinBG");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1f);
        LifeOfLuxuryUIManager.Instance.UpdateButtons("Transition End");

        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);
        yield return new WaitForSeconds(0.5f);
        HideBackground();

        LifeOfLuxuryPaylineController.Instance.StopPaylines();
        LifeOfLuxuryPaylineController.Instance.ClearPaylineData();

        LifeOfLuxuryUIManager.Instance.TextAnimation(LifeOfLuxurySlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);
        LifeOfLuxuryUIManager.Instance.StopMusic("FreeSpinBG");
        lineMultiplierObject.SetActive(false);
        freeSpinsCountText.SetActive(false);
        lineMultiplierTextsObject.SetActive(true);
        LifeOfLuxuryUIManager.Instance.PlaySound("FreeSpinPopup");
        yield return new WaitForSeconds(3.5f);

        LifeOfLuxuryUIManager.Instance.PlayMusic("BG");
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
        // Kill old tweens to avoid fade conflicts
        normalBackgroundImage.DOKill();
        freeSpinBackgroundImage.DOKill();
        normalBackgroundImage1.DOKill();
        freeSpinBackgroundImage1.DOKill();
        normalMachineBgImage.DOKill();
        freeSpinMachineBgImage.DOKill();

        // Make sure free spin objects are active
        freeSpinBackgroundImage.gameObject.SetActive(true);
        freeSpinBackgroundImage1.gameObject.SetActive(true);
        freeSpinMachineBgImage.gameObject.SetActive(true);

        normalBackgroundImage.gameObject.SetActive(true);
        normalBackgroundImage1.gameObject.SetActive(true);
        normalMachineBgImage.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(normalBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(normalBackgroundImage1.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(normalMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));

        seq.Join(freeSpinBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBackgroundImage1.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            normalBackgroundImage.gameObject.SetActive(false);
            normalBackgroundImage1.gameObject.SetActive(false);
            normalMachineBgImage.gameObject.SetActive(false);
        });
    }

    private void HideBackground()
    {
        // Kill old tweens to avoid fade conflicts
        normalBackgroundImage.DOKill();
        freeSpinBackgroundImage.DOKill();
        normalBackgroundImage1.DOKill();
        freeSpinBackgroundImage1.DOKill();
        normalMachineBgImage.DOKill();
        freeSpinMachineBgImage.DOKill();

        // Make sure normal objects are active before fading in
        normalBackgroundImage.gameObject.SetActive(true);
        normalBackgroundImage1.gameObject.SetActive(true);
        normalMachineBgImage.gameObject.SetActive(true);

        freeSpinBackgroundImage.gameObject.SetActive(true);
        freeSpinBackgroundImage1.gameObject.SetActive(true);
        freeSpinMachineBgImage.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(normalBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(normalBackgroundImage1.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(normalMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));

        seq.Join(freeSpinBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBackgroundImage1.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            freeSpinBackgroundImage.gameObject.SetActive(false);
            freeSpinBackgroundImage1.gameObject.SetActive(false);
            freeSpinMachineBgImage.gameObject.SetActive(false);
        });
    }

    #endregion
}