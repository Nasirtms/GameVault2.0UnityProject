using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SaharaRichesFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static SaharaRichesFreeGameTransitionController Instance;

    //[SerializeField] private Image freeSpinBackground;
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    [SerializeField] public GameObject freeSpinsCountText;

    private TMP_Text freeSpinWinText;

    private SaharaRichesFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<SaharaRichesFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        SaharaRichesSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        SaharaRichesSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitUntil(() => SaharaRichesUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitUntil(() => SaharaRichesJackpotAnimator.Instance.isJackpotCompleted);

        SaharaRichesPaylineController.Instance.StopPaylines();
        SaharaRichesPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);
        ShowBackground();
        SaharaRichesUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1f);
        //gameTitle.SetActive(false);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        SaharaRichesUIManager.Instance.UpdateButtons("Free Spin");
        SaharaRichesUIManager.Instance.StopMusic("Background");
        SaharaRichesUIManager.Instance.PlayMusic("FreeSpin_Background");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        SaharaRichesUIManager.Instance.StopMusic("FreeSpin_Background");
        SaharaRichesUIManager.Instance.PlayMusic("Background");

        yield return new WaitForSeconds(1.5f);
        SaharaRichesPaylineController.Instance.StopPaylines();
        SaharaRichesPaylineController.Instance.ClearPaylineData();
        

        yield return new WaitForSeconds(0.5f);
        HideBackground();
        //gameTitle.SetActive(true);
        freeSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        SaharaRichesUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        SaharaRichesUIManager.Instance.TextAnimation(SaharaRichesSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (SaharaRichesSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            SaharaRichesUIManager.Instance.SetAutoInteractable(false);
            SaharaRichesUIManager.Instance.SetSpinInteractable(false);
            WinAnimation();
        }
        else
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Free Spin End");
        }
    }

    private void WinAnimation()
    {
        if (SaharaRichesSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = SaharaRichesSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = SaharaRichesUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, SaharaRichesSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(SaharaRichesSlotMachine.Instance.UpdateGameCoin), 1f);
            SaharaRichesUIManager.Instance.UpdateButtons("Stop");

        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);

        Debug.Log("Popup Animation");
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
