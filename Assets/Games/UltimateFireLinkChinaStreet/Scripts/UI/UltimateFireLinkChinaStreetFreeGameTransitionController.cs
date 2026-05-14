using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UltimateFireLinkChinaStreetFreeGameTransitionController : MonoBehaviour
{
    public static UltimateFireLinkChinaStreetFreeGameTransitionController Instance { get; private set; }

    public float hopHeight = 0.5f;
    public float duration = 0.6f;
    public Ease ease = Ease.OutQuad;

    [SerializeField] private GameObject FreeSpinStart;
    //[SerializeField] private GameObject FreeSpinEnd;
    [SerializeField] private GameObject FreeSpinParent;
    [SerializeField] private TMP_Text freeSpinWin;

    [SerializeField] private Button startFreeSpins;
    [SerializeField] private TMP_Text startFreeSpinsText;
    private Tween startFreeSpinsTween;
    [SerializeField] private Button endFreeSpins;
    [SerializeField] private TMP_Text endFreeSpinsText;
    private Tween endFreeSpinsTween;
    [SerializeField] private GameObject freeSpinParent;
    [SerializeField] private GameObject freeSpin;
    [SerializeField] private SpriteRenderer logo;
    [SerializeField] private Sprite normalLogo;
    [SerializeField] private Sprite freeSpinLogo;
    [SerializeField] private GameObject freeSpinPopupParent;
    [SerializeField] private GameObject freeSpinStartPopup;
    [SerializeField] private GameObject freeSpinEndPopup;
    [SerializeField] private SpriteRenderer slotMachine;
    [SerializeField] private Sprite normalSlotMachine;
    [SerializeField] private Sprite freeSlotMachine;
    //[SerializeField] private SpriteRenderer slotMachineTopCover;
    [SerializeField] private Sprite normalslotMachineTopCover;
    [SerializeField] private Sprite freeslotMachineTopCover;
    //[SerializeField] private SpriteRenderer slotMachineBottomCover;
    [SerializeField] private Sprite normalSlotMachineBottomCover;
    [SerializeField] private Sprite freeSlotMachineBottomCover;
    [SerializeField] private GameObject freeSpinTextPrefabParent;
    [SerializeField] private GameObject freeSpinTextPrefab;
    [SerializeField] private TMP_Text totalFreeSpin;

    private bool canStartFreeSpin = false;
    private bool canEndFreeSpin = false;


    private Animator FreeSpinAnimator;

    private UltimateFireLinkChinaStreetFreeSpinController freeSpinController;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        freeSpinController = GetComponent<UltimateFireLinkChinaStreetFreeSpinController>();
        FreeSpinAnimator = FreeSpinParent.GetComponent<Animator>();
        startFreeSpins.onClick.AddListener(OnClickFreeSpinStart);
        endFreeSpins.onClick.AddListener(OnClickFreeSpinEnd);
    }

    public void StartFreeSpins()
    {
        StartCoroutine(StratFreeSpinTransition());
    }

    public IEnumerator StratFreeSpinTransition()
    {
        canStartFreeSpin = false;
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);
        int x = UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount;
        totalFreeSpin.text = $"<size=60>Total : <size=90><color=#a9ff00>{x}</color></size>  <color=#ff7711>Free Spins</color>  ";
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted);
        UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("FreeSpin");

        freeSpinParent.SetActive(true);
        freeSpin.SetActive(true);

        freeSpinParent.transform.GetComponent<Animator>().SetBool("start", true);
        logo.sprite = freeSpinLogo;
        slotMachine.sprite = freeSlotMachine;
        //slotMachineTopCover.sprite = freeslotMachineTopCover;
        //slotMachineBottomCover.sprite = freeSlotMachineBottomCover;
        yield return new WaitForSeconds(2.8f);
        freeSpinParent.transform.GetComponent<Animator>().SetBool("start", false);

        freeSpin.SetActive(false);

        yield return new WaitForSeconds(1f);

        RectTransform rect = freeSpinStartPopup.transform as RectTransform;
        rect.anchoredPosition = new Vector3(-2000, 0, 0);
        freeSpinPopupParent.SetActive(true);
        freeSpinStartPopup.SetActive(true);
        rect.DOAnchorPosX(0, 1f).SetEase(Ease.OutBack);
        //freeSpinStartPopup.transform.DOMoveX(0, 1f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(2f);
        canStartFreeSpin = true;
        startFreeSpinsTween?.Kill();

        startFreeSpinsTween = startFreeSpinsText.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnClickFreeSpinStart()
    {
        if (!canStartFreeSpin) return;
        freeSpinPopupParent.SetActive(false);
        freeSpinStartPopup.SetActive(false);
        freeSpinParent.SetActive(false);

        freeSpinController.StartFreeSpins();

        UltimateFireLinkChinaStreetUIManager.Instance.PlayMusic("FreeSpinBG");
    }

    public void EndFreeSpin()
    {
        StartCoroutine(EndFreeSpinTransition());
    }

    private IEnumerator EndFreeSpinTransition()
    {
        canEndFreeSpin = false;
        freeSpinParent.SetActive(true);
        freeSpin.SetActive(true);

        freeSpinParent.transform.GetComponent<Animator>().SetBool("end", true);
        logo.sprite = normalLogo;
        slotMachine.sprite = normalSlotMachine;
        //slotMachineTopCover.sprite = normalslotMachineTopCover;
        //slotMachineBottomCover.sprite = normalSlotMachineBottomCover;
        yield return new WaitForSeconds(1.8f);
        freeSpinParent.transform.GetComponent<Animator>().SetBool("end", false);
        freeSpin.SetActive(false);

        freeSpinWin.text = UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinWinAmount.ToString("F2");
        float finalAmount = UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinWinAmount;
        if (finalAmount > 0)
        {
            WinAnimation(finalAmount);
        }
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted);
        RectTransform rect = freeSpinEndPopup.transform as RectTransform;
        rect.anchoredPosition = new Vector3(-2000, 0, 0);
        freeSpinPopupParent.SetActive(true);
        freeSpinEndPopup.SetActive(true);
        rect.DOAnchorPosX(0, 1f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(1f);

        endFreeSpinsTween?.Kill();

        endFreeSpinsTween = endFreeSpinsText.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        canEndFreeSpin = true;
    }

    public void OnClickFreeSpinEnd()
    {
        if (!canEndFreeSpin) return;
        freeSpinPopupParent.SetActive(false);
        freeSpinEndPopup.SetActive(false);
        freeSpinParent.SetActive(false);
    }

    private void WinAnimation(float freegamewin)
    {
        if (freegamewin > 0)
        {
            float betAmount = UltimateFireLinkChinaStreetUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freegamewin, UltimateFireLinkChinaStreetSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(PandaFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }

    public void ReelSwap(Transform ReelA, Transform ReelB)
    {
        Debug.Log("tina is startting the reel swap routine");
        StartCoroutine(ReelSwapRoutine(ReelA, ReelB));
    }

    private IEnumerator ReelSwapRoutine(Transform ReelA, Transform ReelB)
    {
        Vector3 aStart = ReelA.position;
        Vector3 bStart = ReelB.position;

        Tween tweenA = ReelA.DOMove(bStart, duration).SetEase(ease);
        tweenA.OnUpdate(() =>
        {
            float t = tweenA.ElapsedPercentage();

            float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;

            ReelA.position = new Vector3(
                Mathf.Lerp(aStart.x, bStart.x, t),
                Mathf.Lerp(aStart.y, bStart.y, t) + arc,
                aStart.z
            );
        });

        Tween tweenB = ReelB.DOMove(aStart, duration).SetEase(ease);
        tweenB.OnUpdate(() =>
        {
            float t = tweenB.ElapsedPercentage();

            float arc = -Mathf.Sin(t * Mathf.PI) * hopHeight;

            ReelB.position = new Vector3(
                Mathf.Lerp(bStart.x, aStart.x, t),
                Mathf.Lerp(bStart.y, aStart.y, t) + arc,
                bStart.z
            );
        });

        yield return tweenA.WaitForCompletion();
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
}
