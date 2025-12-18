using Sirenix.OdinInspector;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SaharaRichesJackpotAnimator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject jackpotPopup;
    [SerializeField] private Image jackpotFrame;

    [SerializeField] private SaharaRichesJackpotType jackpotType;

    private SaharaRichesJackpotReelSpin saharaRichesJackpotReel;
    public static SaharaRichesJackpotAnimator Instance;
    public bool isJackpotCompleted;

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }
    void Start()
    {
        saharaRichesJackpotReel = GetComponent<SaharaRichesJackpotReelSpin>();
        jackpotPopup.SetActive(false);
    }

    //[ContextMenu("Start Jackpot")]
    public void StartJackpot()
    {
        isJackpotCompleted = false;
        StartCoroutine(ShowJackpot());
    }

    IEnumerator PlayJackpotAnimation()
    {
        isJackpotCompleted = false;
        yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted);
        StartCoroutine("ShowJackpot");

    }

    private IEnumerator ShowJackpot()
    {
        SaharaRichesPaylineController.Instance.StopPaylines();
        SaharaRichesPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(2f);
        jackpotPopup.SetActive(true);
        
        var flag = SaharaRichesSlotMachine.Instance.currentSpinResult.jackpotWin.type;
        if (flag.Contains("mini"))
        {
            jackpotType = SaharaRichesJackpotType.Mini;
        }
        else if (flag.Contains("minor"))
        {
            jackpotType = SaharaRichesJackpotType.Minor;
        }
        else if (flag.Contains("major"))
        {
            jackpotType = SaharaRichesJackpotType.Major;
        }
        else if (flag.Contains("grand"))
        {
            jackpotType = SaharaRichesJackpotType.Grand;
        }
        saharaRichesJackpotReel.StartSpin(jackpotType);

        yield return new WaitUntil(() => !saharaRichesJackpotReel.IsSpinning());
        if (SaharaRichesSlotMachine.Instance.isFreeGameReady || SaharaRichesSlotMachine.Instance.isFreeGame)
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Free Spin");
        }
        else
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Base Game Transition");
        }
            

        yield return new WaitForSeconds(1f);
        jackpotPopup.SetActive(false);
        isJackpotCompleted = true;
        SaharaRichesSlotMachine.Instance.jackpotgame = false;
    }
}
