using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffaloXtraReelPowerFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static BuffaloXtraReelPowerFreeGameTransitionController Instance;

    [SerializeField] private SpriteRenderer baseGameBG;
    [SerializeField] private SpriteRenderer freeSpinBG;
    [SerializeField] private SpriteRenderer baseGameBG1;
    [SerializeField] private SpriteRenderer freeSpinBG1;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject freeSpinCountObject;
    //[SerializeField] private GameObject freeSpinTitle;
    public TMP_Text freeSpinWinText;
    private Coroutine glowCoroutine;

    private bool isFreeSpinStartClick;

    private BuffaloXtraReelPowerFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<BuffaloXtraReelPowerFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        BuffaloXtraReelPowerSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }
    public bool IsFreeSpinStartClicked()
    { 
        return isFreeSpinStartClick;
    }
    public void StartFreeSpinsAfterButtonClick()
    {
        if (!isFreeSpinStartClick) return;

        isFreeSpinStartClick = false;

        BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Free Spin");

        freeSpinController.StartFreeSpins();
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
        yield return new WaitForSeconds(1f);

        BuffaloXtraReelPowerUIManager.Instance.PlaySound("FreeSpinStart");
        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);

        yield return new WaitForSeconds(2.5f);

        StopCoroutine(glowCoroutine);
        PopupAnimation(freeSpinStartFrame, 1f, 1f, false);

        yield return new WaitForSeconds(1f);

        ShowBackground();

        yield return new WaitForSeconds(1f);

        //jackpotTitle.SetActive(false);
        freeSpinCountObject.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1.5f);
        isFreeSpinStartClick = true;
        // Show spin button and allow user to click it
        BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Free Spin Ready");

        //BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Free Spin");
        ////ZombieParadiseUIManager.Instance.PlayMusic("FreeSpin");

        //freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        //ZombieParadiseUIManager.Instance.StopMusic("FreeSpin");
        //ZombieParadiseUIManager.Instance.PlaySound("GameTransition");

        yield return new WaitForSeconds(1.5f);

        HideBackground();

        yield return new WaitForSeconds(0.5f);

        //jackpotTitle.SetActive(true);
        freeSpinCountObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        //BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Transition End");
        //ZombieParadiseUIManager.Instance.PlaySound("FreeSpinWin");

        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);

        BuffaloXtraReelPowerUIManager.Instance.TextAnimation(BuffaloXtraReelPowerSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        StopCoroutine(glowCoroutine);
        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (BuffaloXtraReelPowerSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
    }

    private void WinAnimation()
    {
        if (BuffaloXtraReelPowerSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = BuffaloXtraReelPowerSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = BuffaloXtraReelPowerUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, BuffaloXtraReelPowerSlotMachine.Instance.currentSpinResult.newBalance);

            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Stop");
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.DOKill();

        if (state)
        {
            obj.transform.parent.gameObject.SetActive(true);
            obj.transform.localScale = Vector3.one * 0.5f;

            obj.transform.DOScale(scale, duration).SetEase(Ease.OutBack);
        }
        else
        {
            obj.transform.DOScale(0f, duration).SetEase(Ease.InBack)
            .OnComplete(() =>
                {
                    obj.transform.parent.gameObject.SetActive(false);
                });
        }
    }
    #endregion

    #region Helper Functions

    private void ShowBackground()
    {
        // Kill old tweens to avoid fade conflicts
        baseGameBG.DOKill();
        freeSpinBG.DOKill();
        baseGameBG1.DOKill();
        freeSpinBG1.DOKill();

        // Make sure free spin objects are active
        freeSpinBG.gameObject.SetActive(true);
        freeSpinBG.gameObject.SetActive(true);
        baseGameBG.gameObject.SetActive(true);
        baseGameBG1.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(baseGameBG.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(baseGameBG1.DOFade(0f, 1f).SetEase(Ease.InOutSine));

        seq.Join(freeSpinBG.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBG1.DOFade(1f, 1f).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            baseGameBG.gameObject.SetActive(false);
            baseGameBG1.gameObject.SetActive(false);
        });
    }

    private void HideBackground()
    {
        // Kill old tweens to avoid fade conflicts
        baseGameBG.DOKill();
        freeSpinBG.DOKill();
        baseGameBG1.DOKill();
        freeSpinBG1.DOKill();

        // Make sure normal objects are active before fading in
        baseGameBG.gameObject.SetActive(true);
        baseGameBG1.gameObject.SetActive(true);
        freeSpinBG.gameObject.SetActive(true);
        freeSpinBG1.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(baseGameBG.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(baseGameBG1.DOFade(1f, 1f).SetEase(Ease.InOutSine));

        seq.Join(freeSpinBG.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBG1.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            freeSpinBG.gameObject.SetActive(false);
            freeSpinBG1.gameObject.SetActive(false);
        });
    }
    #endregion
}