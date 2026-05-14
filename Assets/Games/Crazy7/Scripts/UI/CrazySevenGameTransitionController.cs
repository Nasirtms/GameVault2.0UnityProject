using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CrazySevenGameTransitionController : MonoBehaviour
{
    public static CrazySevenGameTransitionController Instance;
    #region variables
    [Header("START POPUP")]
    [SerializeField] private GameObject startPopupRoot;         // parent (Canvas) of the start popup
    [SerializeField] private CanvasGroup startCG;               // fade/blocks raycasts
    [SerializeField] private RectTransform startGlow;           // glowing bg image
    [SerializeField] private RectTransform startLogo;           // "FREE SPINS" sprite
    [SerializeField] private float startPopIn = 0.45f;          // logo pop-in duration
    [SerializeField] private float glowPulseScale = 1.08f;      // glow yoyo scale
    [SerializeField] private float glowPulseTime = 1.2f;        // glow pulse time
    [SerializeField] private float startHoldTime = 1.0f;
    [SerializeField, Range(0.5f, 3f)] private float animScale = 1.6f; 

    [Header("END POPUP")]
    [SerializeField] private GameObject endPopupRoot;           // parent (Canvas) of end popup
    [SerializeField] private CanvasGroup endCG;
    [SerializeField] private RectTransform endGlow;           // glowing bg image
    [SerializeField] private RectTransform endLogo;  // fade/blocks raycasts
    [SerializeField] private RectTransform endFrame;            // frame image that holds amount
    [SerializeField] private TMP_Text endAmountText;            // shows total win (e.g., 5282.558)
    [SerializeField] private TMP_Text tapToContinueText;        // "Tap to continue" label (optional)
    [SerializeField] private float endPopIn = 0.45f;            // frame pop-in duration
    [SerializeField] private float endCountTime = 1.0f;

    [Header("Common")]
    [SerializeField] private Ease popEase = Ease.OutBack;
    private Tween glowLoop;
    private bool isFreeGame;
    private bool endPopupActive;
    private CrazySevenFreeSpinController freeSpinController;
    private CrazySevenSpinSettings spinSettings;
    #endregion
    void Start()
    {
        freeSpinController = GetComponent<CrazySevenFreeSpinController>();
        spinSettings = CrazySevenSlotMachine.Instance.settings.spinSettings;
        isFreeGame = false;

    }
    void Update()

    {
    }
    public void UpdateFreeSpins(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }

    public void StartFreeSpinPop()
    {
        CrazySevenSlotMachine.Instance.isFreeGame = true;
        CrazySevenSlotMachine.Instance.lastFreeSpin = true;
        freeSpinController.ResetFreeSpins();
        StartCoroutine(ShowStartPopup());
    }
    public IEnumerator ShowStartPopup()
    {
        yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => CrazySevenUIManager.Instance.winAnimationCompleted);
        if (!startPopupRoot) yield break;

        // Reset (scaled)
        popEase = Ease.OutBack;
        glowPulseScale = 1.08f;
        glowPulseTime = 1.2f * animScale;  // slower/longer pulse
        startPopIn = 0.45f * animScale;  // longer pop
        endPopIn = 0.45f * animScale;  // keep in sync for symmetry
        endCountTime = 1.0f * animScale;  // used by end popup
        startHoldTime = 1.0f * animScale;  // linger a bit longer
        isFreeGame = !isFreeGame;

        startPopupRoot.SetActive(true);

        if (startCG)
        {
            startCG.alpha = 0f;
            startCG.blocksRaycasts = true;
            startCG.interactable = true;
        }
        if (startLogo) startLogo.localScale = Vector3.zero;

        Image glowImg = null;
        if (startGlow)
        {
            glowImg = startGlow.GetComponent<Image>();
            if (glowImg)
            {
                var c = glowImg.color;
                glowImg.color = new Color(c.r, c.g, c.b, 0f);
            }
            startGlow.localScale = Vector3.one * 0.8f;
        }

        // Sequence for fade + pop
        Sequence s = DOTween.Sequence();

        if (startCG) s.Append(startCG.DOFade(1f, 0.25f * animScale));

        // Make the logo take longer and overshoot just a hair more
        if (startLogo)
        {
            s.Join(
                startLogo
                    .DOScale(1.05f, 0.55f * animScale)            // slightly longer to 105%
                    .SetEase(Ease.OutBack, overshoot: 1.5f)
            )
            .Append(
                startLogo
                    .DOScale(1f, 0.25f * animScale)               // settle to 100%
                    .SetEase(Ease.OutQuad)
            );
        }

        if (glowImg)
        {
            startGlow.DOKill();
            glowLoop?.Kill();

            s.Join(glowImg.DOFade(1f, 0.30f * animScale));
            s.Join(startGlow.DOScale(1f, 0.60f * animScale).SetEase(Ease.OutQuad));

            glowLoop = startGlow
                .DOScale(glowPulseScale, glowPulseTime)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(0.5f * animScale);
        }

        yield return s.WaitForCompletion();
        yield return new WaitForSeconds(startHoldTime);            // longer hold

        if (startCG) yield return startCG.DOFade(0f, 0.30f * animScale).WaitForCompletion();
        glowLoop?.Kill();
        startPopupRoot.SetActive(false);

        //CrazySevenUIManager.Instance.SetButtonsForFreeSpins(true);
        freeSpinController.StartFreeSpins();
    }

    public void EndFreeSpin()
    {
        if (endPopupActive) return;
        endPopupActive = true;
        StartCoroutine(ShowEndPopup(CrazySevenUIManager.Instance.freeGameWinAmount));
    }
    public IEnumerator ShowEndPopup(float totalWin)
    {
        if (!endPopupRoot) yield break;

        yield return new WaitForSeconds(1f);
        InitializeEndPopup();

        yield return AnimatePopupElements(totalWin);
        CrazySevenSlotMachine.Instance.lastFreeSpin = false;
        if (totalWin > 0f) 
        {
            TriggerCoinsAndBanner(totalWin);
        }
        CrazySevenUIManager.Instance.UpdateButtons("FreeSpin Stop");
        CrazySevenSlotMachine.Instance.isFreeGame = false;
        CrazySevenUIManager.Instance.freeGameWinAmount = 0f;
        freeSpinController.ResetFreeSpins();
        endPopupActive = false;
        StartGlowPulse();

        PlayWinAnimations();

        ShowTapToContinue();

        yield return WaitForUserInput();

        yield return CleanupEndPopup();
    }


    private void InitializeEndPopup()
    {
        endPopupRoot.SetActive(true);

        if (endCG)
        {
            endCG.alpha = 0f;
            endCG.blocksRaycasts = true;
            endCG.interactable = true;
        }
        if (endFrame) endFrame.localScale = Vector3.zero;
        if (endAmountText) endAmountText.text = "0";
        if (tapToContinueText) tapToContinueText.alpha = 0f;

        // Reset endGlow
        if (endGlow)
        {
            endGlow.localScale = Vector3.one * 0.8f;
            var glowImg = endGlow.GetComponent<Image>();
            if (glowImg != null)
            {
                var c = glowImg.color;
                glowImg.color = new Color(c.r, c.g, c.b, 0f);
            }
        }

        // Reset endLogo
        if (endLogo)
        {
            endLogo.localScale = Vector3.zero;
            var logoCG = endLogo.GetComponent<CanvasGroup>();
            if (logoCG != null)
            {
                logoCG.alpha = 0f;
            }
            else
            {
                var logoImg = endLogo.GetComponent<Image>();
                if (logoImg != null) logoImg.color = new Color(logoImg.color.r, logoImg.color.g, logoImg.color.b, 0f);
                var logoText = endLogo.GetComponent<TMP_Text>();
                if (logoText != null) logoText.alpha = 0f;
            }
        }
    }

    private IEnumerator AnimatePopupElements(float totalWin)
    {
        Sequence s = DOTween.Sequence();

        if (endCG) s.Append(endCG.DOFade(1f, 0.20f * animScale));

        if (endGlow)
        {
            var glowImg = endGlow.GetComponent<Image>();
            if (glowImg)
            {
                s.Join(glowImg.DOFade(1f, 0.25f * animScale));
                s.Join(endGlow.DOScale(1f, 0.60f * animScale).SetEase(Ease.OutQuad));
            }
        }

        if (endLogo)
        {
            var logoCG = endLogo.GetComponent<CanvasGroup>();
            if (logoCG != null)
            {
                s.Join(logoCG.DOFade(1f, 0.25f * animScale));
            }
            else
            {
                var logoImg = endLogo.GetComponent<Image>();
                if (logoImg != null) s.Join(logoImg.DOFade(1f, 0.25f * animScale));
                var logoText = endLogo.GetComponent<TMP_Text>();
                if (logoText != null) s.Join(logoText.DOFade(1f, 0.25f * animScale));
            }

            s.Join(endLogo.DOScale(1.1f, 0.45f * animScale).SetEase(Ease.OutBack))
             .Join(endLogo.DOScale(1f, 0.2f * animScale).SetEase(Ease.OutQuad));
        }

        if (endFrame)
        {
            RectTransform rt = endFrame as RectTransform;
            Vector2 originalPos = rt.anchoredPosition;

            rt.anchoredPosition = originalPos + new Vector2(0, -100f);

            s.Join(rt.DOAnchorPos(originalPos, 0.45f * animScale).SetEase(Ease.OutBack));
            s.Join(rt.DOScale(1.05f, (endPopIn + 0.10f) * animScale).SetEase(popEase))
             .Join(rt.DOScale(1f, 0.20f * animScale).SetEase(Ease.OutQuad));
        }

        float showTapDelay = Mathf.Max(s.Duration(false) - 0.4f, 0f); 
        DOVirtual.DelayedCall(showTapDelay, () =>
        {
            ShowTapToContinue();
        });

        s.Play();

        if (endAmountText)
        {
            float start = 0f;
            DOTween.To(() => start, v =>
            {
                start = v;
                endAmountText.text = v.ToString("N3");
            }, totalWin, endCountTime * animScale)
            .SetEase(Ease.OutCubic)
            .Play();
        }

        float waitTime = Mathf.Max(s.Duration(false), endCountTime * animScale);
        yield return new WaitForSeconds(waitTime);
    }

    private void StartGlowPulse()
    {
        if (endGlow)
        {
            endGlow.DOKill();
            var glowImg = endGlow.GetComponent<Image>();
            if (glowImg)
            {
                glowLoop?.Kill();
                glowLoop = endGlow
                    .DOScale(glowPulseScale, glowPulseTime)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
    }
    private void PlayWinAnimations()
    {
        if (CrazySevenUIManager.Instance.freeGameWinAmount > 0)
        {
            float freeGameWin = CrazySevenUIManager.Instance.freeGameWinAmount;
            float betAmount = CrazySevenUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, CrazySevenSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(CrazySevenSlotMachine.Instance.UpdateGameCoin), 1f);
            CrazySevenUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }

    private void ShowTapToContinue()
    {
        if (tapToContinueText)
        {
            tapToContinueText.DOFade(1f, 0.30f * animScale);
            tapToContinueText.transform
             .DOScale(1.01f, 2.0f * animScale)
             .SetLoops(-1, LoopType.Yoyo)
             .SetEase(Ease.InOutSine);

        }
    }

    private IEnumerator WaitForUserInput()
    {
        yield return new WaitUntil(UserPressedConfirm);
    }

    private IEnumerator CleanupEndPopup()
    {
        if (tapToContinueText) tapToContinueText.DOKill();
        if (endGlow) glowLoop?.Kill();

        if (endCG) yield return endCG.DOFade(0f, 0.25f * animScale).WaitForCompletion();
        endPopupRoot.SetActive(false);
    }

    private bool UserPressedConfirm()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;

        return false;
    }
    private void TriggerCoinsAndBanner(float win)
    {
        var sm = CrazySevenSlotMachine.Instance;
        var ui = CrazySevenUIManager.Instance;

        // Coins
        if (sm != null && sm.coinManager != null)
            sm.coinManager.BurstCoins();

        // Rolling number
        if (ui != null)
        {
            ui.StopCoinCounter = false;
            ui.PlayCoinTextAnimation(win);
        }

        // Banner text: prefer whatever your textbar shows, fallback to WIN {amount}
        string msg = (sm != null && sm.textbar != null) ? sm.textbar.text : $"WIN {win:0.00}";

        // Banner
        if (sm != null)
            sm.PlayWinBannerOnImage(msg);
    }
}