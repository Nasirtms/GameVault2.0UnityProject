using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickHitVolcanoGameTransitionController : MonoBehaviour
{
    #region Variables
    public static QuickHitVolcanoGameTransitionController Instance { get; private set; }

    [Header("Slot Game")]
    [SerializeField] private GameObject bottomBar;
    [SerializeField] private GameObject slotGame;
    [SerializeField] private Image slotGameBG;
    [SerializeField] private Image slotFrame;

    // Base Game
    [SerializeField] private Sprite baseGameBG;
    [SerializeField] private Sprite baseGameFrame;
    [SerializeField] private GameObject quickHits;
    [SerializeField] private GameObject header;

    // Free Game
    [SerializeField] private Sprite freeGameBG;
    [SerializeField] private Sprite freeGameFrame;
    [SerializeField] private GameObject freeGameReelsHolder;
    [SerializeField] private GameObject freeSpinCount;
    [SerializeField] private GameObject freeSpinWinFrame;

    [Header("Quick Pick Game")]
    [SerializeField] private GameObject quickPickGame;
    [SerializeField] private RectMask2D quickPickMask;
    [SerializeField] private float quickPickMaskDuration = 1f;
    private float quickPickMaskSize;

    [Header("Particles")]
    //[SerializeField] private GameObject fireParticlesLeft;
    [SerializeField] private GameObject fireParticles;
    [SerializeField] private Vector3 positionRight;
    [SerializeField] private Vector3 positionLeft;
    [SerializeField] private float particlesTransitionDuration = 5f;
    //private Tween fireTweenLeft;
    private Tween fireTween;

    private TMP_Text freeSpinWinText;
    private int freeSpins;

    [SerializeField] private QuickHitVolcanoFreeSpinController freeSpinController;

    private Coroutine quickPickTransition;

    #endregion
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        quickPickMaskSize = quickPickMask.gameObject.GetComponent<RectTransform>().rect.size.x;
        Debug.Log("Mask Size: " + quickPickMaskSize);
        freeSpinWinText = freeSpinWinFrame.transform.GetChild(1).GetComponent<TMP_Text>();
    }

    [ContextMenu("Quick Pick Game")]
    public void SevenQuickPick()
    {
        freeSpins = 7;
        quickPickTransition = StartCoroutine(QuickPickGameTransition(7));
    }

    public void StartQuickPickGame(int spinCount)
    {
        freeSpins = spinCount;

        quickPickTransition = StartCoroutine(QuickPickGameTransition(freeSpins));
    }

    [ContextMenu("Free Slot Game")]
    public void StartFreeSlotGame()
    {
        QuickHitVolcanoSlotMachine.Instance.isFreeGame = true;

        if (!QuickHitVolcanoSlotMachine.Instance.extraFreeGame)
        {
            freeSpinController.ResetFreeSpins();
        }

        StartCoroutine(FreeSlotGameTransition(freeSpins));
    }
    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }

    [ContextMenu("Base Slot Game")]
    public void EndFreeSlotGame()
    {
        StartCoroutine(BaseSlotGameTransition());
    }

    private IEnumerator QuickPickGameTransition(int freeSpinsCount)
    {
        yield return new WaitUntil(() => QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted);

        QuickHitVolcanoPaylineController.Instance.StopPaylines();
        QuickHitVolcanoPaylineController.Instance.ClearPaylineData();

        yield return new WaitUntil(() => QuickHitVolcanoUIManager.Instance.winAnimationCompleted);

        foreach (var reel in QuickHitVolcanoSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSortingLayer(0, false);
                    slot.StopAnimation();
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        PlayTransitionParticles(positionRight, positionLeft, particlesTransitionDuration);

        yield return new WaitForSeconds(0.75f);

        quickPickGame.SetActive(true);
        AnimateLeftPadding(quickPickMaskSize, 0f);

        yield return new WaitForSeconds(quickPickMaskDuration);

        slotGame.SetActive(false);
        bottomBar.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        QuickHitVolcanoQuickPickGameManager.Instance.StartQuickPickGame(freeSpinsCount);
    }

    private IEnumerator FreeSlotGameTransition(int freeSpinsCount)
    {
        QuickHitVolcanoUIManager.Instance.PlayMusic("");

        yield return new WaitForSeconds(0.5f);
        
        bottomBar.SetActive(true);
        slotGame.SetActive(true);

        quickHits.SetActive(false);
        header.SetActive(false);
        freeSpinCount.SetActive(true);

        slotGameBG.sprite = freeGameBG;
        slotFrame.sprite = freeGameFrame;
        freeGameReelsHolder.SetActive(true);

        freeSpinController.UpdateFreeSpins(freeSpinsCount);

        if (!QuickHitVolcanoSlotMachine.Instance.extraFreeGame)
        {
            freeSpinController.InitialFreeSpinText();
        }
        else
        {
            freeSpinController.UpdateSpinCount();
        }

        PlayTransitionParticles(positionLeft, positionRight, particlesTransitionDuration);

        yield return new WaitForSeconds(0.75f);

        AnimateLeftPadding(0f, quickPickMaskSize);

        yield return new WaitForSeconds(quickPickMaskDuration);
        
        quickPickGame.SetActive(false);

        yield return new WaitForSeconds(1f);

        if (QuickHitVolcanoSlotMachine.Instance.extraFreeGame)
        {
            QuickHitVolcanoSlotMachine.Instance.extraFreeGame = false;
        }
        else
        {
            freeSpinController.StartFreeSpins();
        }
    }

    private IEnumerator BaseSlotGameTransition()
    {
        QuickHitVolcanoUIManager.Instance.StopMusic("");
        QuickHitVolcanoUIManager.Instance.PlaySound("");

        bottomBar.SetActive(true);
        slotGame.SetActive(true);
        quickPickGame.SetActive(false);

        quickHits.SetActive(true);
        header.SetActive(true);
        freeSpinCount.SetActive(false);

        slotGameBG.sprite = baseGameBG;
        slotFrame.sprite = baseGameFrame;
        freeGameReelsHolder.SetActive(false);

        PopupAnimation(freeSpinWinFrame, 1f, 0.5f, true);

        yield return new WaitForSeconds(1f);

        QuickHitVolcanoUIManager.Instance.TextAnimation(QuickHitVolcanoSlotMachine.Instance.freeSpinWinAmount, 2.5f, freeSpinWinText);

        yield return new WaitForSeconds(3.5f);
        PopupAnimation(freeSpinWinFrame, 0f, 1f, false);
        freeSpinWinText.text = "0.00";

        if (QuickHitVolcanoSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
        }
    }

    private void WinAnimation()
    {
        if (QuickHitVolcanoSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = QuickHitVolcanoSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = QuickHitVolcanoUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, QuickHitVolcanoSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(QuickHitVolcanoSlotMachine.Instance.UpdateGameCoin), 1f);

            QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
        }
    }
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    [ContextMenu("Play Transition Particles")]
    private void PlayTransitionParticles(Vector3 startPosition, Vector3 endPosition, float transitionDuration)
    {
        // Kill any active tweens
        fireTween?.Kill();

        // Get their RectTransforms
        RectTransform firePosition = fireParticles.GetComponent<RectTransform>();

        firePosition.anchoredPosition = startPosition; // reset position

        // Activate both particles
        fireParticles.SetActive(true);

        // Set starting positions
        firePosition.anchoredPosition = startPosition;  // right starts from A → B

        // Animate right particle from A → B
        fireTween = firePosition.DOAnchorPos(endPosition, transitionDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                fireParticles.SetActive(false);
            });
    }

    //private void PlayTransitionParticles()
    //{
    //    // Kill any active tweens
    //    fireTweenLeft?.Kill();
    //    fireTweenRight?.Kill();

    //    // Activate both particles
    //    fireParticlesLeft.SetActive(true);
    //    fireParticlesRight.SetActive(true);

    //    // Get their RectTransforms
    //    RectTransform firePositionLeft = fireParticlesLeft.GetComponent<RectTransform>();
    //    RectTransform firePositionRight = fireParticlesRight.GetComponent<RectTransform>();

    //    // Set starting positions
    //    firePositionLeft.anchoredPosition = positionB;   // left starts from B → A
    //    firePositionRight.anchoredPosition = positionA;  // right starts from A → B

    //    // Animate left particle from B → A
    //    fireTweenLeft = firePositionLeft.DOAnchorPos(positionA, transitionDuration)
    //        .SetEase(Ease.Linear)
    //        .OnComplete(() =>
    //        {
    //            fireParticlesLeft.SetActive(false);
    //            firePositionLeft.anchoredPosition = positionB; // reset for next time
    //        });

    //    // Animate right particle from A → B
    //    fireTweenRight = firePositionRight.DOAnchorPos(positionB, transitionDuration)
    //        .SetEase(Ease.Linear)
    //        .OnComplete(() =>
    //        {
    //            fireParticlesRight.SetActive(false);
    //            firePositionRight.anchoredPosition = positionA; // reset for next time
    //        });
    //}

    public Tween AnimateLeftPadding(float startAmount, float endAmount)
    {
        if (quickPickMask == null) return null;

        // Start with the initial padding
        var padding = quickPickMask.padding;
        padding.x = startAmount;
        quickPickMask.padding = padding;

        // Tween using a temporary float
        return DOTween.To(() => quickPickMask.padding.x,
                          x =>
                          {
                              var p = quickPickMask.padding;
                              p.x = x;
                              quickPickMask.padding = p;
                          },
                          endAmount,
                          quickPickMaskDuration)
                      .SetEase(Ease.Linear);
    }
}
