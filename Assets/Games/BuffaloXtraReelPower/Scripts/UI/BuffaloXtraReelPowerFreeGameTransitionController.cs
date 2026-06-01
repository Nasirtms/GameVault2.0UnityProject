using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffaloXtraReelPowerFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static BuffaloXtraReelPowerFreeGameTransitionController Instance;

    [SerializeField] private SpriteRenderer freeSpinBackground;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject freeSpinCountObject;
    //[SerializeField] private GameObject freeSpinTitle;
    public TMP_Text freeSpinWinText;
    private Coroutine glowCoroutine;

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

        //ZombieParadiseUIManager.Instance.PlaySound("GameTransition");
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(2.5f);

        StopCoroutine(glowCoroutine);
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1f);

        ShowBackground();

        yield return new WaitForSeconds(1f);

        //jackpotTitle.SetActive(false);
        freeSpinCountObject.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1.5f);

        BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Free Spin");
        //ZombieParadiseUIManager.Instance.PlayMusic("FreeSpin");

        freeSpinController.StartFreeSpins();
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

        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

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
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }
    #endregion

    #region Helper Functions

    private void ShowBackground()
    {
        freeSpinBackground.DOFade(1f, 2f);
    }

    private void HideBackground()
    {
        freeSpinBackground.DOFade(0f, 2f);
    }

    #endregion
}