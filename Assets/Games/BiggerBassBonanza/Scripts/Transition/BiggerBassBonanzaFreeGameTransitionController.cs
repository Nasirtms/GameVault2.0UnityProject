using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class BiggerBassBonanzaFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static BiggerBassBonanzaFreeGameTransitionController Instance;

    [Header("General")]
    [SerializeField] private GameObject gameTitle;
    [SerializeField] private GameObject freeSpinHeader;
    [SerializeField] private GameObject backgroundOverlay;
    [SerializeField] private GameObject freeSpinsCountText;

    [Header("Free Spin Start")]
    [SerializeField] private GameObject freeSpinStartPopup;
    [SerializeField] private GameObject[] freeSpins;
    [SerializeField] private Animator freeSpinStartAnimator;
    private int freeSpinCount;

    [Header("Free Spin Retrigger")]
    [SerializeField] private GameObject freeSpinRetriggerPopup;
    [SerializeField] private GameObject[] retriggerMultiplier;

    [Header("Free Spin End")]
    [SerializeField] private GameObject freeSpinEndPopup;
    [SerializeField] private Animator freeSpinEndAnimator;
    [SerializeField] private TMP_Text freeSpinWinText;

    private BiggerBassBonanzaFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<BiggerBassBonanzaFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        BiggerBassBonanzaSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        BiggerBassBonanzaSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("Retrigger")]
    public void RetriggerFreeSpinTransition()
    {
        StartCoroutine(RetriggerFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinCount = freeSpins;
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    public void NetworkErrorFreeSpin()
    {

    }

    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitUntil(() => BiggerBassBonanzaUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted);

        yield return new WaitForSeconds(0.5f);

        BiggerBassBonanzaUIManager.Instance.PlaySound("GameTransition");

        foreach (GameObject freeSpin in freeSpins)
        {
            freeSpin.SetActive(false);
        }
        
        if (freeSpinCount == 10)
        {
            freeSpins[0].SetActive(true);
        }
        else if (freeSpinCount == 15)
        {
            freeSpins[1].SetActive(true);
        }
        else if (freeSpinCount == 20)
        {
            freeSpins[2].SetActive(true);
        }

        backgroundOverlay.SetActive(true);
        PopupAnimation(freeSpinStartPopup, 1f, 1f, true);

        yield return new WaitForSeconds(1f);

        if (freeSpinCount == 10)
        {
            freeSpins[0].SetActive(true);
            freeSpinStartAnimator.SetTrigger("10FreeSpins");
        }
        else if (freeSpinCount == 15)
        {
            freeSpins[1].SetActive(true);
            freeSpinStartAnimator.SetTrigger("15FreeSpins");
        }
        else if (freeSpinCount == 20)
        {
            freeSpins[2].SetActive(true);
            freeSpinStartAnimator.SetTrigger("20FreeSpins");
        }
        
        freeSpinStartAnimator.SetBool("HoldState", true);
        
        Debug.Log("Clip Name: " + freeSpinStartAnimator.runtimeAnimatorController.animationClips[0].name + " Clip length: " + freeSpinStartAnimator.runtimeAnimatorController.animationClips[0].length);
        
        yield return new WaitForSeconds(6f);

        PopupAnimation(freeSpinStartPopup, 0f, 1f, false);
        backgroundOverlay.SetActive(false);

        freeSpinStartAnimator.SetBool("HoldState", false);

        yield return new WaitForSeconds(1f);

        gameTitle.SetActive(false);
        HeaderAnimation(freeSpinHeader, 0.8277087f, 0.5f, true);
        freeSpinsCountText.SetActive(true);

        //freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1f);

        BiggerBassBonanzaUIManager.Instance.UpdateButtons("Free Spin");
        BiggerBassBonanzaUIManager.Instance.PlayMusic("FreeSpin");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator RetriggerFreeSpin()
    {
        BiggerBassBonanzaSlotMachine.Instance.retriggerCount++;

        foreach (GameObject multiplier in retriggerMultiplier)
        {
            multiplier.SetActive(false);
        }

        if (BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 1)
        {
            retriggerMultiplier[0].SetActive(true);
        }
        else if (BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 2)
        {
            retriggerMultiplier[1].SetActive(true);
        }
        else if (BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 3)
        {
            retriggerMultiplier[2].SetActive(true);
        }

        freeSpinController.ResetFreeSpins();
        UpdateFreeSpinsCount(10);

        backgroundOverlay.SetActive(true);
        PopupAnimation(freeSpinRetriggerPopup, 1f, 1f, true);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinRetriggerPopup, 0f, 1f, false);
        backgroundOverlay.SetActive(false);

        yield return new WaitForSeconds(1f);

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        BiggerBassBonanzaUIManager.Instance.StopMusic("FreeSpin");
        BiggerBassBonanzaUIManager.Instance.PlaySound("GameTransition");

        yield return new WaitForSeconds(1.5f);

        backgroundOverlay.SetActive(true);
        PopupAnimation(freeSpinEndPopup, 1f, 1f, true);

        yield return new WaitForSeconds(1f);

        freeSpinEndAnimator.SetTrigger("Play");

        yield return new WaitForSeconds(0.5f);

        BiggerBassBonanzaUIManager.Instance.TextAnimation(BiggerBassBonanzaSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinEndPopup, 0f, 1f, false);
        backgroundOverlay.SetActive(false);

        gameTitle.SetActive(true);
        HeaderAnimation(freeSpinHeader, 0f, 0.5f, false);
        freeSpinsCountText.SetActive(false);

        BiggerBassBonanzaPaylineController.Instance.ResetHeader();

        BiggerBassBonanzaUIManager.Instance.PlaySound("FreeSpinWin");
        
        freeSpinWinText.text = "0.00";

        if (BiggerBassBonanzaSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        //else
        //{
        //    BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
        //}
        
        BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
    }

    private void WinAnimation()
    {
        if (BiggerBassBonanzaSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = BiggerBassBonanzaSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = BiggerBassBonanzaUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, BiggerBassBonanzaSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(BiggerBassBonanzaSlotMachine.Instance.UpdateGameCoin), 1f);
            //else
            //{
            //    BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
            //}
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    private void HeaderAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.SetActive(state);

        obj.transform.localScale = new Vector3(obj.transform.localScale.x, scale * 0.5f, obj.transform.localScale.z);

        obj.transform.DOScaleY(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    #endregion
}
