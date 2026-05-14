using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class FruitMaryGameTransitionController : MonoBehaviour
{
    #region variables

    public static FruitMaryGameTransitionController Instance;

    [Header("START POPUP")]
    [SerializeField] private GameObject freeSpinStart;
    public Animator freeSpinAnimator;

    [Header("END POPUP")]
    [SerializeField] private GameObject freeSpinEnd;
    [SerializeField] private TMP_Text endAmountText;
    public float wait_timer = 2f;
    private bool isFreeGame;
    private bool endPopupActive;
    private FruitMaryFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    void Start()
    {
        freeSpinController = GetComponent<FruitMaryFreeSpinController>();
        isFreeGame = false;
        if (freeSpinStart) freeSpinAnimator = freeSpinStart.GetComponentInParent<Animator>();
    }
    #endregion
    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
    public void StartFreeSpinPopup()
    {
        FruitMarySlotMachine.Instance.isFreeGame = true;
        FruitMarySlotMachine.Instance.lastFreeSpin = true;
        freeSpinController.ResetFreeSpins();
        StartCoroutine(ShowStartPopup());
    }
    public IEnumerator ShowStartPopup()
    {
        yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => FruitMaryUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isBonusGameCompleted);

        FruitMaryPaylineController.Instance.StopPaylines();
        FruitMaryPaylineController.Instance.ClearPaylineData();

        yield return new WaitForSeconds(1f);

        freeSpinStart.SetActive(true);

        yield return null;

        if (freeSpinAnimator == null)
        {
            if (freeSpinStart) freeSpinAnimator = freeSpinStart.GetComponentInParent<Animator>();
        }
        freeSpinAnimator.SetTrigger("FreeSpinStart");

        yield return new WaitForSeconds(wait_timer);

        if (freeSpinController.freeSpinCounterImage)
        {
            freeSpinController.freeSpinCounterImage.SetActive(true);
        }
        freeSpinStart.SetActive(false);
        freeSpinController.InitialFreeSpinText();
        FruitMaryUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    public void EndFreeSpinTransition()
    {
        if (endPopupActive) return;
        endPopupActive = true;
        StartCoroutine(ShowEndPopup());
    }

    public IEnumerator ShowEndPopup()
    {
        if (!freeSpinEnd) yield break;

        yield return new WaitForSeconds(1.5f);
        FruitMaryPaylineController.Instance.StopPaylines();
        FruitMaryPaylineController.Instance.ClearPaylineData();

        freeSpinController.freeSpinCounterImage.SetActive(false);
        freeSpinEnd.SetActive(true);

        if (freeSpinAnimator != null)
        {
            freeSpinAnimator.enabled = true;
            freeSpinAnimator.SetBool("FreeSpinEnd", true); 
        }

        if (endAmountText)
        {
            FruitMaryUIManager.Instance.TextAnimation(FruitMarySlotMachine.Instance.freeSpinWinAmount, 2.5f, endAmountText);
        }

        FruitMarySlotMachine.Instance.lastFreeSpin = false;
        FruitMarySlotMachine.Instance.isFreeGame = false;
        FruitMaryUIManager.Instance.freeGameWinAmount = 0f;
        freeSpinController.ResetFreeSpins();

        if (FruitMarySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            PlayWinAnimations();
        }
        else
        {
            FruitMaryUIManager.Instance.UpdateButtons("Transition End");
        }
        
        yield return new WaitUntil(UserPressedConfirm);

        if (freeSpinAnimator != null)
        {
            freeSpinAnimator.SetBool("FreeSpinEnd", false); // Close animation
        }
        
        yield return new WaitForSeconds(0.5f); // wait for hide animation
        freeSpinEnd.SetActive(false);
        endPopupActive = false;
        endAmountText.text = "0.00";
        FruitMaryUIManager.Instance.UpdateButtons("FreeSpin End");
    }

    private void PlayWinAnimations()
    {
        if (FruitMarySlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = FruitMarySlotMachine.Instance.freeSpinWinAmount;
            float betAmount = FruitMaryUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, FruitMarySlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(FruitMarySlotMachine.Instance.UpdateGameCoin), 1f);
            FruitMaryUIManager.Instance.UpdateButtons("Stop");
        }
    }

    private bool UserPressedConfirm()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;

        return false;
    }
}