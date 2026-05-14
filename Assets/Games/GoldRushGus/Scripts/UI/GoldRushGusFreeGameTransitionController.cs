using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GoldRushGusFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static GoldRushGusFreeGameTransitionController Instance;

    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    public GameObject freeSpinsCountText;

    public TMP_Text totalFreeSpins;
    public TMP_Text totalMultiplier;
    public TMP_Text freeSpinWinText;
    public int totalSpins;
    private GoldRushGusFreeSpinController freeSpinController;
    public bool flag;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<GoldRushGusFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        GoldRushGusSlotMachine.Instance.isFreeGame = true;
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
        totalSpins = freeSpins;
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
        yield return new WaitUntil(() => !GoldRushGusSlotMachine.Instance.isMiniGameRunning);
        yield return new WaitUntil(() => GoldRushGusUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => GoldRushGusUIManager.Instance.isTreasureAnimationCompleted);
        yield return new WaitUntil(() => GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted);

        flag = true;
        totalFreeSpins.text = GoldRushGusSlotMachine.Instance.ToSpriteDigits(totalSpins);
        totalMultiplier.text = $"With {GoldRushGusSlotMachine.Instance.freeSpinMultiplier}x Multiplier ";
        yield return new WaitForSeconds(2f);
        GoldRushGusUIManager.Instance.StopMusic("BG");
        GoldRushGusUIManager.Instance.PlaySound("MiniGamePopup");
        GoldRushGusSlotMachine.Instance.PlayHitAnimation();

        ShowBackground();
        yield return new WaitForSeconds(2f);
        //ShowBackground();

        Animator freeSpinAnimator = GoldRushGusUIManager.Instance.treasureWinAnimations.GetComponent<Animator>();
        GoldRushGusUIManager.Instance.treasureWinAnimations.SetActive(true);
        freeSpinAnimator.enabled = true;
        freeSpinAnimator.SetBool("FreeSpin", true);

        yield return new WaitForSeconds(2.5f);
        GoldRushGusSlotMachine.Instance.SetVillainCharacterNormal();
        yield return new WaitForSeconds(1f);

        GoldRushGusUIManager.Instance.PlayMusic("FreeSpinBG");
        freeSpinAnimator.SetBool("FreeSpin", false);
        freeSpinAnimator.enabled = false;
        GoldRushGusUIManager.Instance.treasureWinAnimations.SetActive(false);
        GoldRushGusPaylineController.Instance.StopPaylines();
        GoldRushGusPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        GoldRushGusUIManager.Instance.UpdateButtons("Free Spin");
        flag = false;
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1.5f);
        GoldRushGusPaylineController.Instance.StopPaylines();
        GoldRushGusPaylineController.Instance.ClearPaylineData();
        GoldRushGusUIManager.Instance.StopMusic("FreeSpinBG");
        yield return new WaitForSeconds(0.5f);
        HideBackground();
        //gameTitle.SetActive(true);
        freeSpinsCountText.SetActive(false);
        GoldRushGusUIManager.Instance.PlaySound("MiniGameEnd");
        yield return new WaitForSeconds(2f);
        GoldRushGusUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        GoldRushGusUIManager.Instance.PlayMusic("BG");

        GoldRushGusUIManager.Instance.TextAnimation(GoldRushGusSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return StartCoroutine(GoldRushGusSlotMachine.Instance.SwapCharacters());
        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (GoldRushGusSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            GoldRushGusUIManager.Instance.SetAutoInteractable(false);
            GoldRushGusUIManager.Instance.SetSpinInteractable(false);
            WinAnimation();
        }
        else
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Free Spin End");
        }
    }

    private void WinAnimation()
    {
        if (GoldRushGusSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = GoldRushGusSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = GoldRushGusUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, GoldRushGusSlotMachine.Instance.currentSpinResult.newBalance);
            GoldRushGusUIManager.Instance.UpdateButtons("Stop");
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