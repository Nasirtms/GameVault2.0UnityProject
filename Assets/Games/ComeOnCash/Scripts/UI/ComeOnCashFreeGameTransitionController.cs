using Coffee.UIEffects;
using DG.Tweening;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComeOnCashFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static ComeOnCashFreeGameTransitionController Instance;

    [SerializeField] private GameObject freeSpin;
    [SerializeField] private GameObject gameParent;
    [SerializeField] private GameObject bonusGame;
    [SerializeField] public TMP_Text offers;
    [SerializeField] private Image freeSpinBg;
    [SerializeField] private Image[] cash;
    [SerializeField] private Image grand;
    [SerializeField] private Image mini;
    [SerializeField] private Image minor;
    [SerializeField] private Sprite bundleOf2;
    [SerializeField] private Sprite bundleOf4;
    [SerializeField] private Sprite bundleOf5;
    [SerializeField] private Sprite bundleOf6;
    [SerializeField] private Sprite bundleOf8;
    [SerializeField] private Sprite bundleGrand;
    [SerializeField] private Sprite bundleMini;
    [SerializeField] private Sprite bundleMinor;
    [SerializeField] private TMP_Text winAmount;

    [SerializeField] private float duration;


    private TMP_Text freeSpinWinText;
    public bool stopRandomGlow = false;
    public bool canEndBonusGame = false;
    private ComeOnCashFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<ComeOnCashFreeSpinController>();
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        ComeOnCashUIManager.Instance.StopMusic("Background");
        ComeOnCashUIManager.Instance.PlayMusic("FreeSpinBackground");
        StartCoroutine(StartFreeSpin());
    }

    public void StartBonusGameTransition(int[] notes, int[] noteIndexes)
    {
        ComeOnCashUIManager.Instance.UpdateButtons("bonusStart");
        //ComeOnCashUIManager.Instance.StopMusic("Background");
        ArrangeNotes(notes);
        StartCoroutine(StartBonusGame(noteIndexes));
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
        if (ComeOnCashSlotMachine.Instance.GetWinAmount() > 0)
        {
            yield return new WaitUntil(() => ComeOnCashUIManager.Instance.winAnimationCompleted);
        }
        yield return new WaitUntil(() => ComeOnCashSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitForSeconds(0.8f);

        freeSpin.SetActive(true);
        freeSpin.GetComponent<Animator>().SetBool("start", true);

        yield return new WaitForSeconds(0.4f);

        freeSpin.GetComponent<Animator>().SetBool("start", false);
        yield return new WaitForSeconds(0.6f);
        freeSpin.SetActive(false);

        BlinkBackground(0.8f);

        ComeOnCashFreeSpinController.Instance.StartFreeSpins();
    }

    private IEnumerator StartBonusGame(int[] noteIndexes)
    {
        ComeOnCashFreeSpinController.Instance.tryAgain.interactable = false;
        ComeOnCashFreeSpinController.Instance.takeOffer.interactable = false;
        if (ComeOnCashSlotMachine.Instance.GetWinAmount() > 0)
        {
            yield return new WaitUntil(() => ComeOnCashUIManager.Instance.winAnimationCompleted);
        }
        yield return new WaitUntil(() => ComeOnCashSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitForSeconds(1.2f);
        BlinkBackground(0.2f);
        offers.text = "0 of 5 OFFERS";
        yield return new WaitForSeconds(1f);
        freeSpinBg.DOKill();
        yield return new WaitForSeconds(0.1f);
        freeSpinBg.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.8f);

        gameParent.GetComponent<Animator>().SetBool("normal", false);
        gameParent.GetComponent<Animator>().SetBool("bonus", true);
        yield return new WaitForSeconds(3f);
        //gameParent.GetComponent<Animator>().SetBool("bonus", false);

        ComeOnCashFreeSpinController.Instance.startBonusGame(noteIndexes, 4f);
    }


    private IEnumerator EndFreeSpin()
    {
        freeSpinBg.DOKill();
        yield return new WaitForSeconds(0.1f);
        freeSpinBg.gameObject.SetActive(false);

        ComeOnCashSlotMachine.Instance.isFreeGame = false;

        if (ComeOnCashSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            ComeOnCashUIManager.Instance.UpdateButtons("exitfreeSpin");
        }

        ComeOnCashUIManager.Instance.PlayMusic("Background");
    }


    public IEnumerator EndBonusGame()
    {
        StartCoroutine(UnGlowSelectedCash(ComeOnCashSlotMachine.Instance.cashIndexes.ToArray(), ComeOnCashSlotMachine.Instance.cashValues.ToArray()));
        yield return new WaitUntil(() => canEndBonusGame);
        yield return new WaitUntil(() => ComeOnCashUIManager.Instance.winAnimationCompleted);
        ComeOnCashSlotMachine.Instance.isBonusGameEnding = false;
        ComeOnCashSlotMachine.Instance.InSpin = false;
        yield return new WaitForSeconds(0.5f);
        gameParent.GetComponent<Animator>().SetBool("bonus", false);
        gameParent.GetComponent<Animator>().SetBool("normal", true);
        yield return new WaitForSeconds(0.75f);
        StopGlowOnCash();
        //gameParent.GetComponent<Animator>().SetBool("normal", false);
        ComeOnCashUIManager.Instance.UpdateButtons("bonusEnd");
        ComeOnCashUIManager.Instance.PlayMusic("Background");
    }

    private void WinAnimation()
    {
        if (ComeOnCashSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = ComeOnCashSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = ComeOnCashUIManager.Instance.CurrentBet();
            ComeOnCashUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;

            if (freeGameWin >= (betAmount * 5000))
            {
                ComeOnCashUIManager.Instance.PlayJackpotWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 500))
            {
                ComeOnCashUIManager.Instance.PlaySuperWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 100))
            {
                ComeOnCashUIManager.Instance.PlayMegaWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 50))
            {
                ComeOnCashUIManager.Instance.PlayBigWinAnimation(freeGameWin);
            }
            else if (freeGameWin >= (betAmount * 10))
            {
                ComeOnCashUIManager.Instance.PlayNiceWinAnimation(freeGameWin);
            }
            else
            {
                ComeOnCashUIManager.Instance.UpdateButtons("Stop");
            }
        }
    }


    private void BlinkBackground(float duration)
    {
        freeSpinBg.gameObject.SetActive(true);

        Color c = freeSpinBg.color;
        c.a = 0.7f;
        freeSpinBg.color = c;

        freeSpinBg
            .DOFade(1f, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void ArrangeNotes(int[] notes)
    {
        for (int i = 0; i < notes.Length; i++)
        {
            switch (notes[i])
            {
                case 2:
                    cash[i].sprite = bundleOf2;
                    break;
                case 4:
                    cash[i].sprite = bundleOf4;
                    break;
                case 5:
                    cash[i].sprite = bundleOf5;
                    break;
                case 6:
                    cash[i].sprite = bundleOf6;
                    break;
                case 8:
                    cash[i].sprite = bundleOf8;
                    break;
                case 20:
                    cash[i].sprite = bundleMinor;
                    break;
                case 100:
                    cash[i].sprite = bundleGrand;
                    break;
                case 10:
                    cash[i].sprite = bundleMini;
                    break;
                default:
                    //cash[i].gameObject.SetActive(false);
                    break;
            }
        }
    }

    public void StartRandomGlow()
    {
        stopRandomGlow = false;
        StartCoroutine(RandomGlow());
    }

    private IEnumerator RandomGlow()
    {
        while (!stopRandomGlow)
        {
            var img = cash[8].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c = img.color;
            c.a = 0f;
            cash[8].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[8].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img1 = cash[4].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c1 = img1.color;
            c1.a = 0f;
            cash[4].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[4].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img2 = cash[5].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c2 = img2.color;
            c2.a = 0f;
            cash[5].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[5].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img3 = cash[9].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c3 = img3.color;
            c3.a = 0f;
            cash[9].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[9].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img4 = grand.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c4 = img4.color;
            c4.a = 0f;
            grand.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            grand.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            yield return new WaitForSeconds(0.4f);
            cash[8].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[4].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[5].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[9].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            grand.gameObject.transform.GetChild(0).gameObject.SetActive(false);

            var img5 = cash[3].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c5 = img5.color;
            c5.a = 0f;
            cash[3].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[3].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img6 = cash[6].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c6 = img6.color;
            c6.a = 0f;
            cash[6].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[6].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img7 = cash[7].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c7 = img7.color;
            c7.a = 0f;
            cash[7].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[7].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img8 = cash[10].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c8 = img8.color;
            c8.a = 0f;
            cash[10].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            cash[10].gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var img9 = mini.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var c9 = img9.color;
            c9.a = 0f;
            mini.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            mini.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            var imga = minor.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>();
            var ca = imga.color;
            ca.a = 0f;
            minor.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            minor.gameObject.transform.GetChild(0).gameObject.transform.GetComponent<Image>().DOFade(1f, 0.4f);
            yield return new WaitForSeconds(0.4f);
            cash[10].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[3].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[6].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[7].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            mini.gameObject.transform.GetChild(0).gameObject.SetActive(false);
            minor.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public IEnumerator GlowTargetCash(int[] notes)
    {
        StopGlowOnCash();
        stopRandomGlow = true;
        for (int i = 0; i < notes.Length; i++)
        {
            ComeOnCashUIManager.Instance.PlaySound("Decrease");
            if (notes[i] == 0)
            {
                bonusGame.transform.DOScale(1.2f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.2f);
                minor.gameObject.transform.GetChild(0).gameObject.SetActive(true);
                minor.gameObject.transform.GetComponent<Canvas>().sortingOrder = 3;
                bonusGame.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.6f);
            }
            else if (notes[i] == 1)
            {
                bonusGame.transform.DOScale(1.2f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.2f);
                grand.gameObject.transform.GetChild(0).gameObject.SetActive(true);
                grand.gameObject.transform.GetComponent<Canvas>().sortingOrder = 3;
                bonusGame.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.6f);
            }
            else if (notes[i] == 2)
            {
                bonusGame.transform.DOScale(1.2f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.2f);
                mini.gameObject.transform.GetChild(0).gameObject.SetActive(true);
                mini.gameObject.transform.GetComponent<Canvas>().sortingOrder = 3;
                bonusGame.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.6f);
            }
            else
            {
                bonusGame.transform.DOScale(1.2f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.2f);
                cash[notes[i]].gameObject.transform.GetChild(0).gameObject.SetActive(true);
                cash[notes[i]].gameObject.transform.GetComponent<Canvas>().sortingOrder = 3;
                bonusGame.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.6f);
            }
        }
        if (ComeOnCashFreeSpinController.Instance.bonusGameOffer < 5)
        {
            ComeOnCashFreeSpinController.Instance.tryAgain.interactable = true;
        }
        else ComeOnCashFreeSpinController.Instance.tryAgain.interactable = false;

        ComeOnCashFreeSpinController.Instance.takeOffer.interactable = true;
    }

    public void StopGlowOnCash()
    {
        for (int i = 0; i < cash.Length; i++)
        {
            cash[i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            cash[i].gameObject.transform.GetComponent<Canvas>().sortingOrder = 1;
        }
        mini.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        mini.gameObject.transform.GetComponent<Canvas>().sortingOrder = 1;
        minor.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        minor.gameObject.transform.GetComponent<Canvas>().sortingOrder = 1;
        grand.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        grand.gameObject.transform.GetComponent<Canvas>().sortingOrder = 1;
    }

    public IEnumerator UnGlowSelectedCash(int[] cashIndex, int[] cashValue)
    {
        canEndBonusGame = false;
        Array.Sort(cashIndex);
        int index = 0;
        float start = 0;
        float end = 0;
        winAmount.text = "0.00";
        for (int i = 0; i < cash.Length; i++)
        {
            if (i == cashIndex[index])
            {
                start = winAmount.text == "" ? 0 : float.Parse(winAmount.text);
                end = start + cashValue[cashIndex[index]];
                index++;
                bonusGame.transform.DOScale(1.2f, 0.2f).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(0.2f);
                cash[i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
                cash[i].gameObject.transform.GetComponent<Canvas>().sortingOrder = 1;
                bonusGame.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
                Debug.Log("Deepak Start : " + start + " End : " + end);
                StartCoroutine(AnimateWinAmount(start, end, winAmount, 0.5f));
                yield return new WaitForSeconds(0.8f);
                if(index == cashIndex.Length)
                {
                    break;
                }
            }
        }
        yield return new WaitForSeconds(1f);
        winAmount.text = "0.00";
        canEndBonusGame = true;
    }

    public IEnumerator AnimateWinAmount(float startAmount, float endAmount, TMP_Text textField, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            // Smooth animation (optional)
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentValue = Mathf.Lerp(startAmount, endAmount, t);

            // Show with 2 decimals
            textField.text = currentValue.ToString("F2");

            yield return null;
        }

        // Ensure exact final value
        textField.text = endAmount.ToString("F2");
    }
        #endregion
}
