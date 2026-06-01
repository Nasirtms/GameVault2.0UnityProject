using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IrishPotLuckJackpotWheelTransition : MonoBehaviour
{
    #region Variables
    public static IrishPotLuckJackpotWheelTransition Instance;

    [Header("Animator")]
    [SerializeField] private GameObject wheel;

    [Header("Popup Frames")]
    [SerializeField] private GameObject wheelStartPopup;
    [SerializeField] private GameObject wheelStartJackpotPopup;
    [SerializeField] private GameObject wheelPopup;
    [SerializeField] private GameObject wheelEndPopup;
    public Animator jackpotWheelAnimator;
    public Animator wheelSpinAnimator;
    public TMP_Text jackpotWinAmount;
    private Image wheelBG;

    private IrishPotLuckJackpotWheelController wheelController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        wheelBG = wheel.GetComponent<Image>();
        jackpotWheelAnimator = wheel.GetComponent<Animator>();
    }

    private void Start()
    {
        wheelController = GetComponent<IrishPotLuckJackpotWheelController>();
        wheelSpinAnimator.enabled = true;
        wheelStartPopup.SetActive(false);
        wheelPopup.SetActive(false);
        wheelEndPopup.SetActive(false);

        if (wheelStartJackpotPopup != null)
            wheelStartJackpotPopup.transform.parent.gameObject.SetActive(false);

        ResetAnimatorBools();
    }

    #endregion

    #region Public References

    [ContextMenu("Start Jackpot Wheel")]
    public void StartJackpotWheelTransition()
    {
        IrishPotLuckSlotMachine.Instance.isJackpotGame = true;
        StartCoroutine(JackpotWheelFlow());
    }
    public void EndJackpotWheelTransition()
    {
        StartCoroutine(EndJackpotWheel());
    }
    #endregion

    #region Jackpot Flow
    private IEnumerator JackpotWheelFlow()
    {
        yield return new WaitUntil(() => IrishPotLuckUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted);

        yield return new WaitForSeconds(1f);
        Debug.Log("LovKumar 4");
        yield return StartCoroutine(ShowWheelStartPopup());

        IrishPotLuckPaylineController.Instance.StopPaylines();
        IrishPotLuckPaylineController.Instance.ClearPaylineData();

        yield return StartCoroutine(ShowWheelPopup());

        yield return StartCoroutine(wheelController.StartJackpotWheelSpin());
        yield return new WaitForSeconds(1f);
        UpdateJackpotWinAmount();
        yield return StartCoroutine(EndJackpotWheel());
    }
    private IEnumerator ShowWheelStartPopup()
    {
        HideAllPopups();
        ResetAnimatorBools();

        wheelStartPopup.SetActive(true);
        wheelBG.enabled = true;
        yield return new WaitForEndOfFrame();
        jackpotWheelAnimator.SetBool("WheelStart", true);

        yield return new WaitForSeconds(2f);

        jackpotWheelAnimator.SetBool("WheelStart", false);
        //Debug.Log("LovLovKumar: jackpotWheelAnimator.GetCurrentAnimatorStateInfo(0).length: " + jackpotWheelAnimator.GetCurrentAnimatorStateInfo(0).length);
        //Debug.Log("LovLovKumar: jackpotWheelAnimator.GetCurrentAnimatorClipInfo(0).Length: " + jackpotWheelAnimator.GetCurrentAnimatorClipInfo(0).Length);
        yield return new WaitForSeconds(jackpotWheelAnimator.GetCurrentAnimatorStateInfo(0).length);
        wheelStartPopup.SetActive(false);
    }

    private IEnumerator ShowWheelPopup()
    {
        HideAllPopups();
        ResetAnimatorBools();

        wheelPopup.SetActive(true);

        wheelSpinAnimator.enabled = true;
        wheelSpinAnimator.SetBool("WheelPopup", true);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(3f);

        PopupAnimation(wheelStartJackpotPopup, 1f, 1f, true);

        yield return new WaitForSeconds(2f);

        PopupAnimation(wheelStartJackpotPopup, 0f, 1f, false);
        wheelSpinAnimator.SetBool("WheelPopup", false);
        wheelSpinAnimator.enabled = false;
    }
    private IEnumerator EndJackpotWheel()
    {
        yield return new WaitForEndOfFrame();
        jackpotWheelAnimator.SetBool("WheelStart", false);
        wheelEndPopup.SetActive(true);
        jackpotWheelAnimator.SetBool("WheelEnd", true);

        yield return new WaitForSeconds(3f);

        jackpotWheelAnimator.SetBool("WheelEnd", false);
        wheelEndPopup.SetActive(false);
        // Now animate WheelPopup out.
        yield return new WaitForSeconds(1.5f);
        wheelBG.enabled = false;
        wheelPopup.SetActive(false);
        if (wheelController.CurrentJackpotWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            IrishPotLuckUIManager.Instance.UpdateButtons("Base Game Transition");
        }

        yield return new WaitUntil(() => IrishPotLuckUIManager.Instance.winAnimationCompleted);

        IrishPotLuckSlotMachine.Instance.isJackpotGame = false;
        IrishPotLuckUIManager.Instance.UpdateButtons("Stop");
    }
    #endregion

    #region Animation Helpers
    private void UpdateJackpotWinAmount()
    {
        if (jackpotWinAmount != null && wheelController != null)
        {
            jackpotWinAmount.text = wheelController.CurrentJackpotWinAmount.ToString("0.00");
        }
    }
    private void WinAnimation()
    {
        if (wheelController.CurrentJackpotWinAmount > 0)
        {
            float jackpotWin = wheelController.CurrentJackpotWinAmount;
            float betAmount = IrishPotLuckUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount,jackpotWin,IrishPotLuckSlotMachine.Instance.currentSpinResult.newBalance);
        }
    }
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    private void ResetAnimatorBools()
    {
        jackpotWheelAnimator.SetBool("WheelStart", false);
        jackpotWheelAnimator.SetBool("WheelEnd", false);
        //wheelSpinAnimator.SetBool("WheelPopup", false);
    }

    private void HideAllPopups()
    {
        wheelStartPopup.SetActive(false);
        //wheelPopup.SetActive(false);
        wheelEndPopup.SetActive(false);

        if (wheelStartJackpotPopup != null)
            wheelStartJackpotPopup.transform.parent.gameObject.SetActive(false);
    }

    #endregion
}