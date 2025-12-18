using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(CleopatraFreeSpinController))]
public class CleopatraGameTransitionController : MonoBehaviour
{
    #region Variables

    public static CleopatraGameTransitionController Instance;

    [Header("Bottom Bars")]
    [SerializeField] private GameObject baseGameBottomBar;
    [SerializeField] private GameObject freeGameBottomBar;
    private bool isFreeGame;

    [Header("Doors")]
    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;
    [SerializeField] private Vector2 leftStartPos;
    [SerializeField] private Vector2 rightStartPos;
    [SerializeField] private Vector2 leftEndPos;
    [SerializeField] private Vector2 rightEndPos;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI transitionText;

    [Header("Animation")]
    [SerializeField] private GameObject leftDoorParticles;
    [SerializeField] private GameObject rightDoorParticles;
    [SerializeField] private Canvas leftCanvas;
    [SerializeField] private Canvas rightCanvas;

    [Header("Timings")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float delayBeforeText = 0.3f;
    [SerializeField] private float delayBeforeAnim = 0.4f;
    [SerializeField] private float holdTextAfterAnim = 0.5f;

    private CleopatraFreeSpinController freeSpinController;
    private CleopatraSpinSettings spinSettings;

    #endregion

    #region Unity Methods

    private void Start()
    {
        freeSpinController = GetComponent<CleopatraFreeSpinController>();
        spinSettings = CleopatraSlotMachine.Instance.settings.spinSettings;

        isFreeGame = false;
        baseGameBottomBar.SetActive(!isFreeGame);
        freeGameBottomBar.SetActive(isFreeGame);
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    #endregion

    #region Public References

    public void PlayTransition()
    {
        StartTransition();
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }

    #endregion

    #region Game Transition

    private void StartTransition()
    {

        // Reset states
        leftDoor.anchoredPosition = leftStartPos;
        rightDoor.anchoredPosition = rightStartPos;
        transitionText.alpha = 0;
        transitionText.gameObject.SetActive(false);
        isFreeGame = !isFreeGame;
        if(!isFreeGame)
            freeSpinController.ResetFreeSpins();

        // Step 1: Set Bottom Bar
        baseGameBottomBar.SetActive(!isFreeGame);
        freeGameBottomBar.SetActive(isFreeGame);
        CleopatraUIManager.Instance.PlaySound("FreeSpinStart");
        if (freeGameBottomBar.activeSelf == true)
            freeSpinController.InitialFreeSpinText();

        // Step 1.5 Set Spin Direction
        if (isFreeGame)
        {
            spinSettings.spinDirection = CleopatraSpinDirection.Random;
        }
        else
        {
            spinSettings.spinDirection = CleopatraSpinDirection.Downwards;
        }

        // Step 2: Move doors in
        Sequence sequence = DOTween.Sequence();
        sequence.Append(leftDoor.DOAnchorPos(leftEndPos, moveDuration).SetEase(Ease.OutQuad));
        sequence.Join(rightDoor.DOAnchorPos(rightEndPos, moveDuration).SetEase(Ease.OutQuad));

        // Step 3: Show text
        if (isFreeGame)
        {
            transitionText.text = $"{CleopatraUIManager.Instance.freeGameSpinCount} Free Spins\nBonus Triggered";
        }
        else
        {
            string winAmount = CleopatraUIManager.Instance.freeGameWinAmount.ToString("N2");
            transitionText.text = $"Congratulations!\nYou Won\n" + winAmount;
        }

        sequence.AppendInterval(delayBeforeText);
        sequence.AppendCallback(() =>
        {
            transitionText.gameObject.SetActive(true);
            transitionText.DOFade(1f, 0.4f);
        });

        // Step 4: After text delay, play animation
        sequence.AppendInterval(delayBeforeAnim);
        sequence.AppendCallback(() =>
        {
            leftCanvas.overrideSorting = true;
            leftCanvas.sortingLayerName = "UI";

            rightCanvas.overrideSorting = true;
            rightCanvas.sortingLayerName = "UI";

            leftDoorParticles.SetActive(true);
            rightDoorParticles.SetActive(true);

            StartCoroutine(WaitForAnimationEnd(2f));
        });
    }

    private IEnumerator WaitForAnimationEnd(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Step 5: Hide text
        transitionText.DOFade(0f, 0.4f).OnComplete(() =>
        {
            transitionText.gameObject.SetActive(false);
        });

        leftCanvas.overrideSorting = false;
        leftCanvas.sortingLayerName = "Default";

        rightCanvas.overrideSorting = false;
        rightCanvas.sortingLayerName = "Default";

        leftDoorParticles.SetActive(false);
        rightDoorParticles.SetActive(false);

        // Step 6: Slide doors back
        leftDoor.DOAnchorPos(leftStartPos, moveDuration).SetEase(Ease.InQuad);
        rightDoor.DOAnchorPos(rightStartPos, moveDuration).SetEase(Ease.InQuad);

        if (isFreeGame)
        {
            //freeSpinController.StartFreeSpins(CleopatraUIManager.Instance.freeGameSpinCount);
            freeSpinController.StartFreeSpins();
            CleopatraUIManager.Instance.StopMusic("Background");
            CleopatraUIManager.Instance.PlayMusic("FreeSpin");
        }
        else
        {
            CleopatraUIManager.Instance.StopMusic("FreeSpin");
            CleopatraUIManager.Instance.PlayMusic("Background");
        }

            yield return new WaitForSeconds(1f);

        if (CleopatraUIManager.Instance.freeGameWinAmount > 0)
        {
            float freeGameWin = CleopatraUIManager.Instance.freeGameWinAmount;
            float betAmount = CleopatraUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, CleopatraSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(CleopatraSlotMachine.Instance.UpdateGameCoin), 1f);
            CleopatraUIManager.Instance.UpdateButtons("Single Stop");
        }
    }

    private float GetAnimationClipLength(Animator animator, string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 1f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        return 1f;
    }

    #endregion
}
