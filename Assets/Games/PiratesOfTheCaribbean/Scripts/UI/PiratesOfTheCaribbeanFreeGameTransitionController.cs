using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PiratesOfTheCaribbeanFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static PiratesOfTheCaribbeanFreeGameTransitionController Instance;

    [SerializeField] private Image freeSpinBackground;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private Animator freeSpinAnimator;
    [SerializeField] private GameObject freeSpinsCountText;
    [SerializeField] private GameObject gameTitle;
    [SerializeField] private GameObject island;
    [SerializeField] private GameObject lighthouse;

    private TMP_Text freeSpinWinText;

    private PiratesOfTheCaribbeanFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<PiratesOfTheCaribbeanFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitUntil(() => PiratesOfTheCaribbeanUIManager.Instance.winAnimationCompleted);
        yield return new WaitForSeconds(1f);

        PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinPop");
        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(0.5f);

        freeSpinAnimator.SetTrigger("FreeSpin");

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 1f, 0.5f, false);

        yield return new WaitForSeconds(1f);

        ShowBackground();
        gameTitle.SetActive(false);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1.5f);

        PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Free Spin");
        PiratesOfTheCaribbeanUIManager.Instance.StopMusic("Background");
        PiratesOfTheCaribbeanUIManager.Instance.PlayMusic("FreeSpin");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        PiratesOfTheCaribbeanUIManager.Instance.StopMusic("FreeSpin");
        PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinPop");
        PiratesOfTheCaribbeanUIManager.Instance.PlayMusic("Background");

        yield return new WaitForSeconds(1.5f);

        HideBackground();

        yield return new WaitForSeconds(0.5f);

        gameTitle.SetActive(true);
        freeSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        PiratesOfTheCaribbeanUIManager.Instance.TextAnimation(PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            PiratesOfTheCaribbeanUIManager.Instance.SetAutoInteractable(false);
            PiratesOfTheCaribbeanUIManager.Instance.SetSpinInteractable(false);

            WinAnimation();
        }
        else
        {
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Free spin End");
        }
    }

    private void WinAnimation()
    {
        if (PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = PiratesOfTheCaribbeanUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, PiratesOfTheCaribbeanSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(PiratesOfTheCaribbeanSlotMachine.Instance.UpdateGameCoin), 1f);
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Single Stop");
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
        freeSpinBackground.DOFade(1f, 2f);
        island.SetActive(false);
        lighthouse.SetActive(true);
        lighthouse.GetComponent<Animator>().SetBool("LightHouse", true);
    }

    private void HideBackground()
    {
        freeSpinBackground.DOFade(0f, 2f);
        island.SetActive(true);
        lighthouse.GetComponent<Animator>().SetBool("LightHouse", false);
        lighthouse.SetActive(false);
    }

    #endregion
}
