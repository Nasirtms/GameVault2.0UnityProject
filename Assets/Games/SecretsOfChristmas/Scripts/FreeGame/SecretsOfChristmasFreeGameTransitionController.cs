//using DG.Tweening;
//using System.Collections;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class SecretsOfChristmasFreeGameTransitionController : MonoBehaviour
//{
//    #region Variables

//    public static SecretsOfChristmasFreeGameTransitionController Instance;

//    [Header("Backgrounds")]
//    [SerializeField] private SpriteRenderer freeSpinBackground;
//    [SerializeField] private SpriteRenderer bonusBackground;

//    [Header("Free Spin Start")]
//    [SerializeField] private GameObject freeSpinStartFrame;

//    [Header("Free Spin UI")]
//    [SerializeField] private GameObject freeSpinWinFrame;
//    [SerializeField] private GameObject freeSpinCountObject;
//    public TMP_Text freeSpinWinText;

//    [Header("Start Bonus Game")]
//    [SerializeField] private GameObject startBonusPopup;
//    [SerializeField] private Button startBonusButton;

//    [Header("Bonus End Reward")]
//    [SerializeField] private GameObject bonusEndPopup;
//    [SerializeField] private TMP_Text bonusRewardText;
//    [SerializeField] private Button bonusEndContinueButton;

//    private SecretsOfChristmasFreeSpinController freeSpinController;
//    private int pendingScatterCount;
//    private int pendingBaseFreeSpins;
//    #endregion

//    #region Unity Methods

//    private void Awake()
//    {
//        if (Instance == null) { Instance = this; }
//        else { Destroy(gameObject); }
//    }

//    private void Start()
//    {
//        freeSpinController = GetComponent<SecretsOfChristmasFreeSpinController>();

//        freeSpinStartFrame.SetActive(false);
//        startBonusPopup.SetActive(false);
//        bonusEndPopup.SetActive(false);
//        freeSpinWinFrame.SetActive(false);

//        startBonusButton.onClick.AddListener(OnClickStartBonus);
//        bonusEndContinueButton.onClick.AddListener(OnClickBonusEndContinue);
//    }

//    #endregion

//    #region Public References

//    [ContextMenu("Start")]

//    public void StartFreeSpinFeatureFlow(int scatterCount, int baseFreeSpins)
//    {
//        pendingScatterCount = scatterCount;
//        pendingBaseFreeSpins = baseFreeSpins;

//        SecretsOfChristmasSlotMachine.Instance.isFreeGame = true;
//        SecretsOfChristmasUIManager.Instance.UpdateButtons("Transition Start");

//        StartCoroutine(FreeSpinWonPopupRoutine());
//    }
//    public void StartFreeSpinTransition()
//    {
//        SecretsOfChristmasSlotMachine.Instance.isFreeGame = true;
//        freeSpinController.ResetFreeSpins();

//        StartCoroutine(StartFreeSpin());
//    }
//    public void ShowBonusEndPopup(string rewardSummary)
//    {
//        bonusRewardText.text = rewardSummary;
//        PopupAnimation(bonusEndPopup, true);
//    }

//    public void UpdateFreeSpinsCount(int freeSpins)
//    {
//        freeSpinController.UpdateFreeSpins(freeSpins);
//    }
//    public void NetworkErrorFreeSpin()
//    {
//        freeSpinController.ErrorFreeSpinReturn();
//    }
//    [ContextMenu("End")]
//    public void EndFreeSpinTransition()
//    {
//        StartCoroutine(EndFreeSpin());
//    }
//    #endregion

//    #region Game Transition

//    private IEnumerator StartFreeSpin()
//    {
//        yield return new WaitForSeconds(1f);

//        //ZombieParadiseUIManager.Instance.PlaySound("GameTransition");
//        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);

//        yield return new WaitForSeconds(2.5f);

//        //StopCoroutine(glowCoroutine);
//        PopupAnimation(freeSpinStartFrame, 0f, 1f, false);

//        yield return new WaitForSeconds(1f);

//        ShowBonusBackground();

//        yield return new WaitForSeconds(1f);

//        //jackpotTitle.SetActive(false);
//        freeSpinCountObject.SetActive(true);
//        freeSpinController.InitialFreeSpinText();

//        yield return new WaitForSeconds(1.5f);

//        SecretsOfChristmasUIManager.Instance.UpdateButtons("Free Spin");
//        //ZombieParadiseUIManager.Instance.PlayMusic("FreeSpin");

//        freeSpinController.StartFreeSpins();
//    }

//    private IEnumerator EndFreeSpin()
//    {
//        //ZombieParadiseUIManager.Instance.StopMusic("FreeSpin");
//        //ZombieParadiseUIManager.Instance.PlaySound("GameTransition");

//        yield return new WaitForSeconds(1.5f);

//        HideBackground();

//        yield return new WaitForSeconds(0.5f);

//        //jackpotTitle.SetActive(true);
//        freeSpinCountObject.SetActive(false);

//        yield return new WaitForSeconds(2f);

//        //BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Transition End");
//        //ZombieParadiseUIManager.Instance.PlaySound("FreeSpinWin");

//        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);

//        yield return new WaitForSeconds(1f);

//        SecretsOfChristmasUIManager.Instance.TextAnimation(SecretsOfChristmasSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

//        yield return new WaitForSeconds(3.5f);

//        //StopCoroutine(glowCoroutine);
//        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

//        freeSpinWinText.text = "0.00";

//        if (SecretsOfChristmasSlotMachine.Instance.freeSpinWinAmount > 0)
//        {
//            WinAnimation();
//        }
//    }

//    private void WinAnimation()
//    {
//        if (SecretsOfChristmasSlotMachine.Instance.freeSpinWinAmount > 0)
//        {
//            float freeGameWin = SecretsOfChristmasSlotMachine.Instance.freeSpinWinAmount;
//            float betAmount = SecretsOfChristmasUIManager.Instance.CurrentBet();

//            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, SecretsOfChristmasSlotMachine.Instance.currentSpinResult.newBalance);

//            SecretsOfChristmasUIManager.Instance.UpdateButtons("Stop");
//        }
//    }

//    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
//    {
//        obj.transform.parent.gameObject.SetActive(state);

//        obj.transform.localScale = Vector3.one * 0.5f;

//        obj.transform.DOScale(scale, duration * 1.2f)
//            .SetEase(Ease.OutBack);
//    }
//    #endregion

//    #region Helper Functions

//    private void ShowBackground()
//    {
//        freeSpinBackground.DOFade(1f, 2f);
//    }
//    private void ShowBonusBackground()
//    {
//        bonusBackground.DOFade(1f, 2f);
//    }

//    private void HideBackground()
//    {
//        freeSpinBackground.DOFade(0f, 2f);
//    }

//    #endregion
//}