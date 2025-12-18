using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VegasSevenFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static VegasSevenFreeGameTransitionController Instance;

    [SerializeField] GameObject freeSpinObject;
    [SerializeField] GameObject winFreeScreenPanel;
    [SerializeField] TextMeshProUGUI winAmounttext;
    [SerializeField] TextMeshProUGUI freeGamesText;
    [SerializeField] TextMeshProUGUI secondFreeGamesText;
    [SerializeField] ChilliMultiplyerUI chilliMultiplyerUI;

    private VegasSevenFreeSpinController freeSpinController;

    [Header("SlotsFrame")]

    [SerializeField] Image slotsFrame;

    [SerializeField] Image normalFrame;
    [SerializeField] Image freePlayFrame;
    private bool isBaseAvail = true;
    [SerializeField]float fadeDuration = 0.65f;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<VegasSevenFreeSpinController>();
        normalFrame.color = new Color(1, 1, 1, 1);
        freePlayFrame.color = new Color(1, 1, 1, 0);
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        VegasSevenSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        VegasSevenSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();
        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        yield return new WaitUntil(() => VegasSevenUIManager.Instance.winAnimationCompleted);
        VegasSevenPaylineController.Instance.StopPaylineLoop();
        VegasSevenPaylineController.Instance.ClearPaylineResults(); 
        yield return new WaitForSeconds(0.5f);
        freeGamesText.text = secondFreeGamesText.text = VegasSevenSlotMachine.Instance.freeSpinCount.ToString();
        freeSpinObject.SetActive(true);
        freeSpinObject.GetComponent<Animator>().SetBool("freespin", true);
        // Sari Transactions k bad


        yield return new WaitForSeconds(1.5f);
        freeSpinObject.SetActive(false);
        freeSpinObject.GetComponent<Animator>().SetBool("freespin", false);
        freeSpinController.StartFreeSpins();
        ToggleFade();

        VegasSevenUIManager.Instance.StopMusic("Background");
        VegasSevenUIManager.Instance.PlayMusic("FreeSpin");
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }

    private IEnumerator EndFreeSpin()
    {
        VegasSevenPaylineController.Instance.StopPaylineLoop();
        VegasSevenPaylineController.Instance.ClearPaylineResults();
        DisableChilli();

        yield return new WaitForSeconds(0.5f);
        ToggleFade();
        VegasSevenUIManager.Instance.StopMusic("FreeSpin");
        VegasSevenUIManager.Instance.PlayMusic("Background");

     

        if (VegasSevenSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            winFreeScreenPanel.SetActive(true);
            winFreeScreenPanel.GetComponent<Animator>().SetBool("freespinwin", true);
            VegasSevenUIManager.Instance.PlayWinAnimationText(VegasSevenSlotMachine.Instance.freeSpinWinAmount, 1, winAmounttext);
            yield return new WaitForSeconds(2.75f);
            winFreeScreenPanel.SetActive(false);
            winFreeScreenPanel.GetComponent<Animator>().SetBool("freespinwin", false);

            WinAnimation();
        }
        else
        {
            VegasSevenUIManager.Instance.UpdateButtons("Default");
        }
    }

    private void WinAnimation()
    {
        if (VegasSevenSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = VegasSevenSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = VegasSevenUIManager.Instance.CurrentBet();


            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, VegasSevenSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(VegasSevenSlotMachine.Instance.UpdateGameCoin), 1f);

            VegasSevenUIManager.Instance.UpdateButtons("Default");
        }
    }
    [ContextMenu("ChangeColor")]
    public void ChangeColor()
    {
        ToggleFade();
    }

    public void ToggleFade()
    {
        if (isBaseAvail)
        {
            normalFrame.DOFade(0f, fadeDuration);
            freePlayFrame.DOFade(1f, fadeDuration);
        }
        else
        {
            freePlayFrame.DOFade(0f, fadeDuration);
            normalFrame.DOFade(1f, fadeDuration);
        }


        isBaseAvail = !isBaseAvail;
    }

    public void SetChilliUI(int count) {

        chilliMultiplyerUI.gameObject.SetActive(true);
        chilliMultiplyerUI.SetData(count);
    }
    public void DisableChilli() {
        chilliMultiplyerUI.PlayReverse();
    }

    #endregion
}
