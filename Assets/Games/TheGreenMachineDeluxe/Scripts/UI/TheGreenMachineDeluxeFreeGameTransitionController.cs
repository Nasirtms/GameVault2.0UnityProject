using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class TheGreenMachineDeluxeFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static TheGreenMachineDeluxeFreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpinBackground;
    [SerializeField] private UITransitionEffect gameTransitionEffect;
    [SerializeField] private ParticleSystem cashParticles;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject topBarJackpotImage;
    [SerializeField] private GameObject freeSpinsCountText;
    private TMP_Text freeSpinWinText;

    private TheGreenMachineDeluxeFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<TheGreenMachineDeluxeFreeSpinController>();
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(0).GetComponent<TMP_Text>();    
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        TheGreenMachineDeluxeSlotMachine.Instance.isFreeGame = true;
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
        TheGreenMachineDeluxeUIManager.Instance.StopMusic("BG");
        TheGreenMachineDeluxeUIManager.Instance.PlaySound("GameTransition");
        yield return new WaitUntil(() => TheGreenMachineDeluxeUIManager.Instance.winAnimationCompleted);
        yield return new WaitForSeconds(0.5f);

        PlayCashParticles();

        yield return new WaitForSeconds(1.5f);

        ShowBackground();

        yield return new WaitForSeconds(1.35f);

        topBarJackpotImage.SetActive(false);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(0.15f);

        StopCashParticles();

        yield return new WaitForSeconds(1f);

        TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Free Spin");
        TheGreenMachineDeluxeUIManager.Instance.PlayMusic("FreeSpin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        TheGreenMachineDeluxeUIManager.Instance.StopMusic("FreeSpin");

        TheGreenMachineDeluxeUIManager.Instance.PlaySound("GameTransition");

        yield return new WaitForSeconds(0.5f);

        PlayCashParticles();

        yield return new WaitForSeconds(1.5f);

        HideBackground();

        yield return new WaitForSeconds(0.5f);
        TheGreenMachineDeluxeUIManager.Instance.PlayMusic("BG");
        topBarJackpotImage.SetActive(true);
        freeSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Base Game Transition");
        TheGreenMachineDeluxeUIManager.Instance.PlaySound("FreeSpinWin");
        FreeSpinWinAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        TheGreenMachineDeluxeUIManager.Instance.TextAnimation(TheGreenMachineDeluxeSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        FreeSpinWinAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";
        if (TheGreenMachineDeluxeSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = TheGreenMachineDeluxeSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = TheGreenMachineDeluxeUIManager.Instance.CurrentBet();

            //GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, TheGreenMachineDeluxeSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(TheGreenMachineDeluxeSlotMachine.Instance.UpdateGameCoin), 1f);

            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Base Game Transition");
        }
        else
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Base Game Transition");
        }
    }

    private void FreeSpinWinAnimation(GameObject obj, float scale, float duration, bool state)
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
        if (gameTransitionEffect != null)
        {
            gameTransitionEffect.Show();
        }
    }

    private void HideBackground()
    {
        if (gameTransitionEffect != null)
        {
            gameTransitionEffect.Hide();
        }
    }

    private void PlayCashParticles()
    {
        cashParticles.Play();

    }
    
    private void StopCashParticles()
    {
        cashParticles.Stop();
    }

    #endregion
}
