using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IrishPotLuckFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static IrishPotLuckFreeGameTransitionController Instance;

    [SerializeField] private SpriteRenderer normalBackgroundImage;
    [SerializeField] private SpriteRenderer freeSpinBackgroundImage;

    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject freeSpinEndFrame;
    [SerializeField] private Animator freeSpinAnimator;

    [SerializeField] private Button freeSpinStartContinueButton;
    [SerializeField] private Button freeSpinEndContinueButton;
    public GameObject freeSpinsCount;
    public TMP_Text freeSpinsCountText;
    public TMP_Text freeSpinWinText;

    private IrishPotLuckFreeSpinController freeSpinController;
    private bool continueClicked;
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

        freeSpinStartFrame.SetActive(false);
        freeSpinEndFrame.SetActive(false);
        freeSpinStartContinueButton.onClick.AddListener(OnContinueButtonClicked);
        freeSpinEndContinueButton.onClick.AddListener(OnContinueButtonClicked);
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
    public void OnContinueButtonClicked()
    {
        continueClicked = true;
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

        freeSpinsCountText.text = $"{IrishPotLuckSlotMachine.Instance.freeSpinCount}";
        ShowBackground();

        continueClicked = false;
        freeSpinStartFrame.SetActive(true);
        freeSpinAnimator.SetBool("FreeSpinStart", true);

        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => continueClicked);

        freeSpinAnimator.SetBool("FreeSpinStart", false);
        freeSpinStartFrame.SetActive(false);

        freeSpinsCount.SetActive(true);
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
        freeSpinsCount.SetActive(false);

        yield return new WaitForSeconds(2f);

        IrishPotLuckUIManager.Instance.UpdateButtons("Transition End");
        continueClicked = false;

        freeSpinStartFrame.SetActive(false);
        freeSpinEndFrame.SetActive(true);
        freeSpinAnimator.SetBool("FreeSpinEnd", true);

        yield return new WaitForSeconds(1f);

        IrishPotLuckUIManager.Instance.TextAnimation(IrishPotLuckSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(() => continueClicked);

        freeSpinAnimator.SetBool("FreeSpinEnd", false);
        freeSpinEndFrame.SetActive(false);

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