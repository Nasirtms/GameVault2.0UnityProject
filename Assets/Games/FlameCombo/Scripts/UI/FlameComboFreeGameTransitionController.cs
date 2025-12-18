using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlameComboFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    [Header("Doors")]
    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;
    [SerializeField] private Vector2 leftStartPos;
    [SerializeField] private Vector2 rightStartPos;
    [SerializeField] private Vector2 leftEndPos;
    [SerializeField] private Vector2 rightEndPos;
    [SerializeField] private float doorMoveDuration = 1f;

    [Header("Shake")]
    [SerializeField] private float defaultDuration = 0.25f;
    [SerializeField] private float defaultMagnitude = 20f; // pixels
    [SerializeField] private RectTransform canvasTransform;
    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    [SerializeField] private GameObject freeSpinsCountText;
    [SerializeField] private GameObject baseGameFrame;
    [SerializeField] private GameObject freeGameFrame;
    [SerializeField] private TMP_Text freeSpinWinText;
    public static FlameComboFreeGameTransitionController Instance;
    private FlameComboFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<FlameComboFreeSpinController>();
    }
    #endregion

    #region Public References
    public void StartFreeSpinTransition()
    {
        FlameComboSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }
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
        yield return new WaitUntil(() => FlameComboSlotMachine.Instance.isPaylineCompleted);
        yield return new WaitUntil(() => FlameComboUIManager.Instance.winAnimationCompleted);

        yield return new WaitForSeconds(1f);
        FlameComboPaylineController.Instance.ClearPaylineData();

        yield return MoveDoors();

        baseGameFrame.SetActive(false);
        freeGameFrame.SetActive(true);

        yield return new WaitForSeconds(1.5f);
        FlameComboUIManager.Instance.StopTitleLoop();
        freeSpinsCountText.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        yield return new WaitForSeconds(1f);

        FlameComboUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(2f);

        freeSpinsCountText.SetActive(false);
        freeSpinWinText.gameObject.SetActive(true);
        FlameComboPaylineController.Instance.ClearPaylineData();
        FlameComboUIManager.Instance.UpdateButtons("Transition End");
        FlameComboUIManager.Instance.TextAnimation(FlameComboSlotMachine.Instance.freeSpinWinAmount, 3f, freeSpinWinText);

        yield return new WaitForSeconds(2.5f);
        yield return MoveDoors();
        baseGameFrame.SetActive(true);
        freeGameFrame.SetActive(false);

        yield return new WaitForSeconds(1f);

        freeSpinWinText.text = "0.00";
        freeSpinWinText.gameObject.SetActive(false);
        FlameComboUIManager.Instance.StartTitleLoop();

        if (FlameComboSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            FlameComboUIManager.Instance.spinButton.GetComponent<Button>().interactable = false;
            WinAnimation();
        }
        FlameComboUIManager.Instance.spinButton.GetComponent<Button>().interactable = true;
    }
    private void WinAnimation()
    {
        if (FlameComboSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = FlameComboSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = FlameComboUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, FlameComboSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(FlameComboSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }
    #endregion

    #region Helper Functions
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

        //collisionParticles.SetActive(false);
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
        //collisionParticles.SetActive(true);

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
