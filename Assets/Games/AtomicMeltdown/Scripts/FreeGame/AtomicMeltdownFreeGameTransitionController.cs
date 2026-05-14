using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class AtomicMeltdownFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static AtomicMeltdownFreeGameTransitionController Instance;

    [Header("Doors")]
    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;
    [SerializeField] private Vector2 leftStartPos;
    [SerializeField] private Vector2 rightStartPos;
    [SerializeField] private Vector2 leftEndPos;
    [SerializeField] private Vector2 rightEndPos;
    [SerializeField] private float doorMoveDuration = 1f;

    [Header("Lights & Particles")]
    [SerializeField] private Image machineGlow;
    [SerializeField] private Image lightLeft;
    [SerializeField] private Image lightRight;
    [SerializeField] private GameObject freeSpinParticles;

    [Header("Chains")]
    [SerializeField] private RectTransform chainLeft;
    [SerializeField] private RectTransform chainRight;
    [SerializeField] private Vector3 positionOneLeftChain = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 positionTwoLeftChain = new Vector3(5f, 0f, 0f);
    [SerializeField] private Vector3 positionOneRightChain = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 positionTwoRightChain = new Vector3(5f, 0f, 0f);
    [SerializeField] private float duration = 2f;
    [SerializeField] private GameObject collisionParticles;
    [SerializeField] private GameObject paylines;

    [Header("Shake")]
    [SerializeField] private float defaultDuration = 0.25f;
    [SerializeField] private float defaultMagnitude = 20f; // pixels
    [SerializeField] private RectTransform canvasTransform;
    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    private Tween machineGlowTween;
    private Tween lightLeftTween;
    private Tween lightRightTween;
    private Tween chainLeftTween;
    private Tween chainRightTween;

    private AtomicMeltdownFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<AtomicMeltdownFreeSpinController>();
        originalPos = canvasTransform.anchoredPosition;
    }

    #endregion

    #region Public References

    [ContextMenu("3 Free Spins")]
    public void ThreeFreeSpins()
    {
        AtomicMeltdownSlotMachine.Instance.isFreeGame = true;
        StartFreeSpinTransition();
        UpdateFreeSpinsCount(3);
    }

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        AtomicMeltdownSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitUntil(() => AtomicMeltdownUIManager.Instance.winAnimationCompleted);

        yield return new WaitForSeconds(0.5f);

        AtomicMeltdownPaylineController.Instance.StopPaylineLoop();
        AtomicMeltdownPaylineController.Instance.ClearPaylineResults();

        paylines.SetActive(false);
        AtomicMeltdownUIManager.Instance.StopMusic("BG");
        AtomicMeltdownUIManager.Instance.PlaySound("FreeSpinStart");
        yield return MoveDoors();

        StartChains();
        StartOpacityLoop();
        freeSpinParticles.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        paylines.SetActive(true);

        AtomicMeltdownUIManager.Instance.PlayMusic("FreeSpin");
        yield return new WaitForSeconds(1.5f);

        freeSpinController.StartFreeSpins();     
    }

    private IEnumerator EndFreeSpin()
    {
        AtomicMeltdownPaylineController.Instance.StopPaylineLoop();
        AtomicMeltdownPaylineController.Instance.ClearPaylineResults();

        yield return new WaitForSeconds(0.5f);

        AtomicMeltdownUIManager.Instance.StopMusic("FreeSpin");

        AtomicMeltdownUIManager.Instance.PlayMusic("BG");
        paylines.SetActive(false);

        yield return MoveDoors();

        StopChains();
        StopOpacityLoop();
        freeSpinParticles.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        paylines.SetActive(true);

        if (AtomicMeltdownSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
        }
    }

    private void WinAnimation()
    {
        if (AtomicMeltdownSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = AtomicMeltdownSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = AtomicMeltdownUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, AtomicMeltdownSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(AtomicMeltdownSlotMachine.Instance.UpdateGameCoin), 1f);
            AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
        }
    }

    public void StartOpacityLoop()
    {
        // Kill any previous tween to avoid overlaps
        machineGlowTween?.Kill();
        lightLeftTween?.Kill();
        lightRightTween?.Kill();

        // Start tween from current alpha to maxAlpha
        machineGlowTween = machineGlow
            .DOFade(1, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)        // Loop forever (ping-pong between min and max)
            .SetEase(Ease.InOutSine)            // Smooth transition
            .From(0);                    // Start from minAlpha

        lightLeftTween = lightLeft
            .DOFade(1, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)        // Loop forever (ping-pong between min and max)
            .SetEase(Ease.InOutSine)            // Smooth transition
            .From(0);                    // Start from minAlpha

        lightRightTween = lightRight
            .DOFade(1, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)        // Loop forever (ping-pong between min and max)
            .SetEase(Ease.InOutSine)            // Smooth transition
            .From(0);                    // Start from minAlpha
    }

    public void StopOpacityLoop()
    {
        machineGlowTween?.Kill();
        lightLeftTween?.Kill();
        lightRightTween?.Kill();

        machineGlowTween = null;
        lightLeftTween = null;
        lightRightTween = null;

        machineGlow.color = new Color(1f, 1f, 1f, 0f);
        lightLeft.color = new Color(1f, 1f, 1f, 0f);
        lightRight.color = new Color(1f, 1f, 1f, 0f);
    }

    [ContextMenu("Start Chain")]
    public void StartChains()
    {
        // Stop any previous tween
        chainLeftTween?.Kill();
        chainRightTween?.Kill();

        // Ensure the object starts at positionOne
        chainLeft.anchoredPosition = positionOneLeftChain;
        chainRight.anchoredPosition = positionOneRightChain;

        // Tween to positionTwo and loop back to positionOne
        chainLeftTween = chainLeft.DOAnchorPos(positionTwoLeftChain, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart); // Ping-pong between the two positions forever

        chainRightTween = chainRight.DOAnchorPos(positionTwoRightChain, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart); // Ping-pong between the two positions forever
    }

    [ContextMenu("Stop Chain")]
    public void StopChains()
    {
        chainLeftTween?.Kill();
        chainRightTween?.Kill();

        chainLeftTween = null;
        chainRightTween = null;

        chainLeft.anchoredPosition = positionOneLeftChain;
        chainRight.anchoredPosition = positionOneRightChain;
    }

    public IEnumerator MoveDoors()
    {
        leftDoor.anchoredPosition = leftStartPos;
        rightDoor.anchoredPosition = rightStartPos;

        yield return new WaitForSeconds(0.5f);

        leftDoor.DOAnchorPos(leftEndPos, doorMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Shake());

        rightDoor.DOAnchorPos(rightEndPos, doorMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Shake());

        yield return new WaitForSeconds(2.5f);

        leftDoor.DOAnchorPos(leftStartPos, doorMoveDuration).SetEase(Ease.InQuad);
        rightDoor.DOAnchorPos(rightStartPos, doorMoveDuration).SetEase(Ease.InQuad);

        collisionParticles.SetActive(false);
    }

    public void Shake() => Shake(defaultDuration, defaultMagnitude);

    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            canvasTransform.anchoredPosition = originalPos;
        }
        shakeRoutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        collisionParticles.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            canvasTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasTransform.anchoredPosition = originalPos;
        shakeRoutine = null;
    }

    #endregion
}
