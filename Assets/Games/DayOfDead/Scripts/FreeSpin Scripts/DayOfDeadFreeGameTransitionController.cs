using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayOfDeadFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static DayOfDeadFreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpinBackground;
    private SpriteRenderer freeSpinBG_Sprite;
    [SerializeField] public SpriteRenderer baseGameBG_Sprite1;
    [SerializeField] public SpriteRenderer freeSpinBG_Sprite1;

    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private TMP_Text freeSpinWinText;

    private DayOfDeadFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<DayOfDeadFreeSpinController>();
        freeSpinBG_Sprite = freeSpinBackground.GetComponent<SpriteRenderer>();
    }
    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        DayOfDeadSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }
    public void MoveWildtoReel()
    {
        StartCoroutine(freeSpinController.MoveWildFromTopbarForSpin());
    }
    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitUntil(() => DayOfDeadUIManager.Instance.winAnimationCompleted);
        //yield return new WaitForSeconds(2f);

        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinPop");
        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);
        DayOfDeadPaylineController.Instance.StopPaylines();
        DayOfDeadPaylineController.Instance.ClearPaylineData();
        freeSpinController.topbar.topbarArea.SetActive(true);
        ShowBackground();
        yield return new WaitForSeconds(1.5f);

        yield return new WaitUntil(UserPressedConfirm);
        PopupAnimation(freeSpinStartFrame, 0f, 1f, false);

        yield return new WaitForSeconds(1f);

        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1.5f);

        DayOfDeadUIManager.Instance.UpdateButtons("Free Spin");
        //DayOfDeadUIManager.Instance.StopMusic("Background");
        //DayOfDeadUIManager.Instance.PlayMusic("FreeSpin");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        //DayOfDeadUIManager.Instance.StopMusic("FreeSpin");
        //DayOfDeadUIManager.Instance.PlaySound("FreeSpinPop");
        //DayOfDeadUIManager.Instance.PlayMusic("Background");

        //yield return new WaitForSeconds(1.5f);

        yield return new WaitForSeconds(1f);
        DayOfDeadUIManager.Instance.UpdateButtons("Free Spin End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);
        HideBackground();
        DayOfDeadUIManager.Instance.TextAnimation(DayOfDeadSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);
        if (DayOfDeadSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            DayOfDeadUIManager.Instance.SetAutoInteractable(false);
            DayOfDeadUIManager.Instance.SetSpinInteractable(false);

            WinAnimation();
        }
        else
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Free Spin End");
        }
        yield return new WaitUntil(UserPressedConfirm);
        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);
        DayOfDeadPaylineController.Instance.StopPaylines();
        DayOfDeadPaylineController.Instance.ClearPaylineData();
        freeSpinWinText.text = "0.00";
    }

    private void WinAnimation()
    {
        if (DayOfDeadSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = DayOfDeadSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = DayOfDeadUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, DayOfDeadSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(DayOfDeadSlotMachine.Instance.UpdateGameCoin), 1f);

            DayOfDeadUIManager.Instance.UpdateButtons("Single Stop");
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.02f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    #endregion

    #region Helper Functions
    private bool UserPressedConfirm()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;

        return false;
    }
    private void ShowBackground()
    {
        freeSpinBackground.SetActive(true);
        freeSpinBG_Sprite.DOFade(1f, 2.8f)
            .SetEase(Ease.OutQuad);
        freeSpinBG_Sprite1.DOFade(1f, 2.8f)
            .SetEase(Ease.OutQuad);
        baseGameBG_Sprite1.DOFade(0f, 2.8f)
            .SetEase(Ease.OutQuad);
    }

    private void HideBackground()
    {
        freeSpinBG_Sprite.DOFade(0f, 2.8f)
            .SetEase(Ease.OutQuad);
        baseGameBG_Sprite1.DOFade(1f, 2.8f)
            .SetEase(Ease.OutQuad);
        freeSpinBG_Sprite1.DOFade(0f, 2.8f)
            .SetEase(Ease.OutQuad);
        freeSpinBackground.SetActive(false);
    }
    #endregion
}