using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombieParadiseFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static ZombieParadiseFreeGameTransitionController Instance;

    [SerializeField] private Image freeSpinBackground;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject jackpotTitle;
    [SerializeField] private GameObject freeSpinTitle;
    [SerializeField] private Image spinStartGlow;
    [SerializeField] private Image spinEndGlow;

    private TMP_Text freeSpinWinText;
    private Coroutine glowCoroutine;

    private ZombieParadiseFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<ZombieParadiseFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        ZombieParadiseSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        ZombieParadiseSlotMachine.Instance.isFreeGame = true;
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
        glowCoroutine = StartCoroutine(Glow(spinStartGlow));

        yield return new WaitForSeconds(2.5f);

        StopCoroutine(glowCoroutine);
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1f);

        ShowBackground();

        yield return new WaitForSeconds(1f);

        jackpotTitle.SetActive(false);
        freeSpinTitle.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1.5f);

        ZombieParadiseUIManager.Instance.UpdateButtons("Free Spin");
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

        jackpotTitle.SetActive(true);
        freeSpinTitle.SetActive(false);

        yield return new WaitForSeconds(2f);

        ZombieParadiseUIManager.Instance.UpdateButtons("Transition End");
        //ZombieParadiseUIManager.Instance.PlaySound("FreeSpinWin");

        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);
        glowCoroutine = StartCoroutine(Glow(spinEndGlow));

        yield return new WaitForSeconds(1f);

        ZombieParadiseUIManager.Instance.TextAnimation(ZombieParadiseSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        StopCoroutine(glowCoroutine);
        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (ZombieParadiseSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
    }

    private void WinAnimation()
    {
        if (ZombieParadiseSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = ZombieParadiseSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = ZombieParadiseUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, ZombieParadiseSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(ZombieParadiseSlotMachine.Instance.UpdateGameCoin), 1f);
            ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    private IEnumerator Glow(Image glow)
    {
        while (true)
        {
            glow.DOFade(1f, 1f);

            yield return new WaitForSeconds(0.25f);

            glow.DOFade(0f, 1f);

            yield return new WaitForSeconds(0.25f);
        }
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
