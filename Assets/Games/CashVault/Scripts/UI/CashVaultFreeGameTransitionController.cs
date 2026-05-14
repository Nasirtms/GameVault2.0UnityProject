using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CashVaultFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static CashVaultFreeGameTransitionController Instance;

    //[SerializeField] private Image freeSpinBackground;
    //[SerializeField] private Image normalBackgroundImage;
    //[SerializeField] private Image freeSpinBackgroundImage;
    [SerializeField] private GameObject freeSpinWinFrame;
    [SerializeField] private GameObject freeSpinStartFrame;
    //[SerializeField] private Animator freeSpinAnimator;
    [SerializeField] public GameObject freeSpinsCountText;
    [SerializeField] private TMP_Text freeSpinWinText;
    public TMP_Text totalFreeSpins;
    private CashVaultFreeSpinController freeSpinController;
    public int totalSpins;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        //originalAnchoredPosition = freeSpinReelBox.anchoredPosition;
    }

    private void Start()
    {
        freeSpinController = GetComponent<CashVaultFreeSpinController>();

        //RectTransform maskedContainer = freeSpinReelBox.transform.parent.GetComponent<RectTransform>();
        //maskedContainerHeight = maskedContainer.rect.height;
    }

    #endregion

    #region Public References

    public void StartFreeSpinTransition()
    {
        CashVaultSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        totalSpins = freeSpins;
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
        //yield return new WaitUntil(() => CashVaultUIManager.Instance.winAnimationCompleted);
        //yield return new WaitUntil(() => CashVaultSlotMachine.Instance.isSlotAnimationCompleted);

        //CashVaultPaylineController.Instance.StopPaylines();
        //CashVaultPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);
        //ShowBackground();
        //SaharaRichesUIManager.Instance.PlaySound("FreeSpinPopup");
        Debug.Log("Lov Kumar 9");
        totalFreeSpins.text = $"{totalSpins} TOTAL SPINS WITH {CashVaultSlotMachine.Instance.wildCount} WILDS";
        PopupAnimation(freeSpinStartFrame, 1f, 1f, true);

        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(freeSpinStartFrame, 0f, 1f, false);

        yield return new WaitForSeconds(1f);
        //gameTitle.SetActive(false);
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        CashVaultUIManager.Instance.UpdateButtons("Free Spin");
        //SaharaRichesUIManager.Instance.StopMusic("Background");
        //SaharaRichesUIManager.Instance.PlayMusic("FreeSpin_Background");
        //StartJackpot();
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        //SaharaRichesUIManager.Instance.StopMusic("FreeSpin_Background");
        //SaharaRichesUIManager.Instance.PlayMusic("Background");
        yield return new WaitForSeconds(1.5f);
        CashVaultPaylineController.Instance.StopPaylines();
        CashVaultPaylineController.Instance.ClearPaylineData();

        yield return new WaitForSeconds(0.5f);
        //HideBackground();
        //gameTitle.SetActive(true);
        freeSpinsCountText.SetActive(false);

        yield return new WaitForSeconds(2f);

        CashVaultUIManager.Instance.UpdateButtons("Transition End");
        //PiratesOfTheCaribbeanUIManager.Instance.PlaySound("FreeSpinWin");
        PopupAnimation(freeSpinWinFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);

        CashVaultUIManager.Instance.TextAnimation(CashVaultSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);

        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);

        freeSpinWinText.text = "0.00";

        if (CashVaultSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            CashVaultUIManager.Instance.SetAutoInteractable(false);
            CashVaultUIManager.Instance.SetSpinInteractable(false);
            WinAnimation();
        }
        else
        {
            CashVaultUIManager.Instance.UpdateButtons("Free Spin End");
        }
    }

    private void WinAnimation()
    {
        if (CashVaultSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = CashVaultSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = CashVaultUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, CashVaultSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(SaharaRichesSlotMachine.Instance.UpdateGameCoin), 1f);
            CashVaultUIManager.Instance.UpdateButtons("Stop");
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

    private void ShowBackground()
    {
        //Sequence seq = DOTween.Sequence();
        //normalBackgroundImage.gameObject.SetActive(true);
        //freeSpinBackgroundImage.gameObject.SetActive(true);

        //seq.Append(normalBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
        //seq.Join(freeSpinBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
    }

    private void HideBackground()
    {
        //Sequence seq = DOTween.Sequence();
        //normalBackgroundImage.gameObject.SetActive(true);
        //freeSpinBackgroundImage.gameObject.SetActive(true);

        //seq.Append(normalBackgroundImage.DOFade(1f, 1f).SetEase(Ease.InOutSine));
        //seq.Join(freeSpinBackgroundImage.DOFade(0f, 1f).SetEase(Ease.InOutSine));
    }
    #endregion

    //#region FreeSpins Reel
    //[ContextMenu("Start Jackpot")]
    //public void StartJackpot()
    //{
    //    isFreeSpinCompleted = false;
    //    StartCoroutine(ShowJackpot());
    //}
    //private IEnumerator ShowJackpot()
    //{
    //    yield return new WaitForSeconds(1f);

    //    //if (CashVaultSlotMachine.Instance.freeSpinCount == 6)
    //    //{
    //    //    freeSpinCount = freeSpins.Six;
    //    //}
    //    //else if(CashVaultSlotMachine.Instance.freeSpinCount == 9)
    //    //{
    //    //    freeSpinCount = freeSpins.Nine;
    //    //}
    //    //else if (CashVaultSlotMachine.Instance.freeSpinCount == 12)
    //    //{
    //    //    freeSpinCount = freeSpins.Twelve;
    //    //}
    //    //else
    //    //{
    //    //    freeSpinCount = freeSpins.Fifteen;
    //    //}
    //    //if (CashVaultSlotMachine.Instance.wildCount == 50)
    //    //{
    //    //    wildSpinCount = wildSpins.Fifty;
    //    //}
    //    //else if (CashVaultSlotMachine.Instance.freeSpinCount == 9)
    //    //{
    //    //    wildSpinCount = wildSpins.Hundred;
    //    //}
    //    //else if (CashVaultSlotMachine.Instance.freeSpinCount == 12)
    //    //{
    //    //    wildSpinCount = wildSpins.OneFifty;
    //    //}
    //    //else
    //    //{
    //    //    wildSpinCount = wildSpins.TwoHundred;
    //    //}

    //    StartSpin(freeSpinReelBox, (int)freeSpinCount);

    //    yield return new WaitUntil(() => !IsSpinning());

    //    yield return new WaitForSeconds(1.5f);
    //    StartSpin(wildReelBox, (int)wildSpinCount);
    //    yield return new WaitUntil(() => !IsSpinning());
    //    isFreeSpinCompleted = true;
    //}
    //public void StartSpin(RectTransform reelBox, int resultIndex)
    //{
    //    if (!isSpinning)
    //        StartCoroutine(SpinRoutine(reelBox, resultIndex));
    //}
    //IEnumerator SpinRoutine(RectTransform reelBox, int targetIndex)
    //{
    //    float spinDuration = Random.Range(minSpinDuration, maxSpinDuration);
    //    float scrollSpeed = Random.Range(minScrollSpeed, maxScrollSpeed);

    //    isSpinning = true;

    //    reelBox.anchoredPosition = originalAnchoredPosition;

    //    float currentY = originalAnchoredPosition.y;
    //    float elapsed = 0f;

    //    float loopHeight = 4 * slotHeight;

    //    while (elapsed < spinDuration)
    //    {
    //        currentY -= scrollSpeed * Time.deltaTime;

    //        if (currentY <= originalAnchoredPosition.y - loopHeight)
    //        {
    //            currentY += loopHeight;
    //        }

    //        reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, currentY);

    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    float stopTime = Random.Range(minStopTime, maxStopTime);
    //    float t = 0f;
    //    float startY = reelBox.anchoredPosition.y;
    //    float targetY = GetTargetY(targetIndex);

    //    while (targetY > startY)
    //    {
    //        targetY -= loopHeight;
    //    }

    //    while (t < stopTime)
    //    {
    //        float eased = EaseOutCubic(t / stopTime);
    //        float y = Mathf.Lerp(startY, targetY, eased);

    //        reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, y);

    //        t += Time.deltaTime;
    //        yield return null;
    //    }

    //    reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, targetY);
    //    isSpinning = false;
    //}

    ////IEnumerator SpinRoutine(int targetIndex)
    ////{
    ////    float spinDuration = Random.Range(minSpinDuration, maxSpinDuration);
    ////    float scrollSpeed = Random.Range(minScrollSpeed, maxScrollSpeed);

    ////    isSpinning = true;

    ////    float currentY = 0f;
    ////    float elapsed = 0f;

    ////    while (elapsed < spinDuration)
    ////    {
    ////        currentY -= scrollSpeed * Time.deltaTime;

    ////        if (currentY <= -reelHeight) //  / 2
    ////        {
    ////            currentY = reelHeight;
    ////            //currentY = 0;
    ////        }

    ////        prizeReel.anchoredPosition = new Vector2(0, currentY);
    ////        elapsed += Time.deltaTime;
    ////        yield return null;
    ////    }

    ////    float stopTime = Random.Range(minStopTime, maxStopTime);
    ////    float t = 0f;
    ////    float startY = currentY;

    ////    int stopIndex = targetIndex; //  + 4
    ////    Debug.Log("Stop Index : " + stopIndex);
    ////    float targetY = -stopIndex * slotHeight + maskedContainerHeight; // / 3f
    ////    Debug.Log("Target y : " + targetY);

    ////    while (t < stopTime)
    ////    {
    ////        float eased = EaseOutCubic(t / stopTime);

    ////        if (prizeReel.anchoredPosition.y <= -reelHeight && stopIndex % 4 == 3)  //  / 2
    ////        {
    ////            startY = 0;
    ////            prizeReel.anchoredPosition = new Vector2(0, startY);
    ////            targetY = -targetIndex * slotHeight + maskedContainerHeight; //  / 3f
    ////        }

    ////        float interpY = Mathf.Lerp(startY, targetY, eased);
    ////        prizeReel.anchoredPosition = new Vector2(0, interpY);

    ////        t += Time.deltaTime;

    ////        yield return null;
    ////    }

    ////    prizeReel.anchoredPosition = new Vector2(0, targetY);
    ////    isSpinning = false;
    ////}

    //private float EaseOutCubic(float t)
    //{
    //    return 1f - Mathf.Pow(1f - t, 3);
    //}

    //public bool IsSpinning() => isSpinning;

    //#endregion
}