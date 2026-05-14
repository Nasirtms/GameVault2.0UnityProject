using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StickyPiggyFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static StickyPiggyFreeGameTransitionController Instance;

    [SerializeField] private Image normalBgImage;
    [SerializeField] private Image freeSpinBgImage;
    [SerializeField] private SpriteRenderer normalMachineBgImage;
    [SerializeField] private SpriteRenderer freeSpinMachineBgImage;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject freeSpinWinFrame;
    public GameObject freeSpinsCountText;

    public TMP_Text freeSpinWinText;
    public TMP_Text totalSpinsCountText;

    private StickyPiggyFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<StickyPiggyFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        StickyPiggySlotMachine.Instance.isFreeGame = true;
        //freeSpinController.ResetFreeSpins();
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
        totalSpinsCountText.text = ToSpriteDigits(StickyPiggyFreeSpinLocker.Instance.totalSpins);

        ShowBackground();
        //GoldRushGusUIManager.Instance.PlaySound("FreeSpinPopup");
        PopupAnimation(freeSpinStartFrame, 2f, 0.5f, true);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 0f, 0.5f, false);

        yield return new WaitForSeconds(1f);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        StickyPiggyUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1.5f);
        StickyPiggyPaylineController.Instance.StopPaylines();
        StickyPiggyPaylineController.Instance.ClearPaylineData();

        yield return new WaitForSeconds(2f);
        StickyPiggySlotMachine.Instance.ClearWildInstances();
        StickyPiggyUIManager.Instance.UpdateButtons("Transition End");

        PopupAnimation(freeSpinWinFrame, 2f, 0.5f, true);
        StickyPiggyUIManager.Instance.TextAnimation(StickyPiggySlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText, true);
        HideBackground();
        freeSpinsCountText.SetActive(false);
        StickyPiggyFreeSpinLocker.Instance.ResetFreeSpinHeader();

        yield return new WaitForSeconds(3.5f);
        StickyPiggyUIManager.Instance.StopMusic("FreeSpinBg");
        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (StickyPiggySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            StickyPiggyUIManager.Instance.SetStopInteractable(false);
            WinAnimation();
        }
        else
        {
            StickyPiggyUIManager.Instance.UpdateButtons("Free Spin End");
        }
        StickyPiggyUIManager.Instance.PlayMusic("BG");
    }

    private void WinAnimation()
    {
        if (StickyPiggySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = StickyPiggySlotMachine.Instance.freeSpinWinAmount;
            float betAmount = StickyPiggyUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, StickyPiggySlotMachine.Instance.currentSpinResult.newBalance);
            StickyPiggyUIManager.Instance.UpdateButtons("Stop");
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
    private string ToSpriteDigits(int value)
    {
        string s = value.ToString();
        StringBuilder sb = new StringBuilder(s.Length * 10);

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (ch >= '0' && ch <= '9')
                sb.Append($"<sprite index={ch - '0'}>");
        }

        return sb.ToString();
    }
    private void ShowBackground()
    {
        Sequence seq = DOTween.Sequence();
        //normalBgImage.gameObject.SetActive(true);
        //freeSpinBgImage.gameObject.SetActive(true);

        seq.Append(normalBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Append(normalMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }

    private void HideBackground()
    {
        Sequence seq = DOTween.Sequence();
        //normalBgImage.gameObject.SetActive(true);
        //freeSpinBgImage.gameObject.SetActive(true);
        seq.Append(normalBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Join(freeSpinMachineBgImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        seq.Append(normalMachineBgImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }
    #endregion
}