using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UltimateFireLinkChinaStreetPaylineController : MonoBehaviour
{
    #region Variables

    public static UltimateFireLinkChinaStreetPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<UltimateFireLinkChinaStreetPaylineData> paylines;
    //[SerializeField] private PandaFortuneFreeSpinController freeSpin;
    // Currently running paylines
    public List<UltimateFireLinkChinaStreetPaylineEntry> activePaylines = new List<UltimateFireLinkChinaStreetPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true

    [SerializeField] public GameObject freeSpinParent;
    [SerializeField] private GameObject freeSpinStart;
    [SerializeField] public GameObject freeSpinEnd;
    [SerializeField] private Button freeSpinButton20;
    [SerializeField] private Button freeSpinButton15;
    [SerializeField] private Button freeSpinButton10;
    [SerializeField] private Button freeSpinButton7;
    [SerializeField] private Button freeSpinButton5;
    [SerializeField] private Button freeSpinButtonMystery;
    [SerializeField] public GameObject freeSpinTypeParent;
    [SerializeField] public GameObject freeSpin20;
    [SerializeField] public GameObject freeSpin15;
    [SerializeField] public GameObject freeSpin10;
    [SerializeField] public GameObject freeSpin7;
    [SerializeField] public GameObject freeSpin5;
    [SerializeField] public GameObject freeSpinMystery;
    [SerializeField] public GameObject wildMultiplier;
    [SerializeField] public GameObject jackpots;
    [SerializeField] public GameObject ads;
    [SerializeField] public GameObject freeSpinCountObj;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<UltimateFireLinkChinaStreetPaylineResult> spinResult = new List<UltimateFireLinkChinaStreetPaylineResult>();
    private int resultScatterCount;

    //[SerializeField] public GameObject Target;
    //[SerializeField] public TMP_Text Target_Text;
    //[SerializeField] private GameObject overlay;
    //[SerializeField] public GameObject FreeSpinTarget;


    //[HideInInspector] public List<PandaFortuneSlotScript> freeSpinSlots;
    private int freeSpinTotal;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        freeSpinButton20.onClick.AddListener(() => StartFreeSpins(20));
        freeSpinButton15.onClick.AddListener(() => StartFreeSpins(15));
        freeSpinButton10.onClick.AddListener(() => StartFreeSpins(10));
        freeSpinButton7.onClick.AddListener(() => StartFreeSpins(7));
        freeSpinButton5.onClick.AddListener(() => StartFreeSpins(5));
        freeSpinButtonMystery.onClick.AddListener(() => StartFreeSpins(-1));
    }

    #endregion

    #region Public References
    public void StartPayline(int scatterCount)
    {
        resultScatterCount = scatterCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(UltimateFireLinkChinaStreetPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        ResetAllSlotsToDefault();
        spinResult.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<UltimateFireLinkChinaStreetPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new UltimateFireLinkChinaStreetPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText,
                    result.symbol
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 1)
        {
            //overlay.SetActive(false);
            UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted = true;

            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        if (scatterAnimation != null)
        {
            StopCoroutine(scatterAnimation);
            scatterAnimation = null;
        }

        ResetAllSlotsToDefault();

        //overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        //SaharaRichesUIManager.Instance.PlaySound("Payline");
        //if (!SaharaRichesAutoSpinController.isAutoSpinning && !SaharaRichesSlotMachine.Instance.isFreeGame)
        //{
        //    SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;
        //}
        if (UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGame)
        {
            flickerDelay = 1.5f;
        }
        else
        {
            flickerDelay = 2.5f;
        }
        //overlay.SetActive(true);

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }


        if (activePaylines.Count == 0)
        {
            UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                Invoke("PaylineAnimationCompleted", flickerDelay);

            }
            else
            {
                int i = 0;
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        i++;
                        yield return null;
                        yield return PlaySinglePayline(entry);
                    }
                    if (activePaylines.Count == i)
                    {
                        Invoke("PaylineAnimationCompleted", flickerDelay);
                    }
                }
            }
        }

    }

    public void PaylineAnimationCompleted()
    {
        isShowing = false;
        UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(UltimateFireLinkChinaStreetPaylineEntry entry)
    {
        for (int x = 0; x < UltimateFireLinkChinaStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkChinaStreetSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        for (int x = 0; x < UltimateFireLinkChinaStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkChinaStreetSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.StopAnimation();
                }
            }
        }

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in UltimateFireLinkChinaStreetSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    //slot.SetSpriteToDefault();
                    slot.StopAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < UltimateFireLinkChinaStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkChinaStreetSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == UltimateFireLinkChinaStreetSlotType.Wild)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);


        if (!UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGame && UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGameReady)
        {
            UltimateFireLinkChinaStreetSlotMachine.Instance.firstFreeSpin = true;
            StartCoroutine(ShowFreeSpinButtons());
        }
        else if (UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount > 0 && UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGame)
        {
            UltimateFireLinkChinaStreetFreeGameTransitionController.Instance.UpdateFreeSpinsCount(UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount);
        }
    }

    private IEnumerator ShowFreeSpinButtons()
    {
        freeSpinStart.transform.localScale = Vector3.zero;
        freeSpinParent.SetActive(true);
        freeSpinStart.SetActive(true);
        freeSpinStart.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);

        freeSpinButton20.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        freeSpinButton15.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        freeSpinButton10.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        freeSpinButton7.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        freeSpinButton5.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        freeSpinButtonMystery.transform.DOScale(0.8f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StartFreeSpins(int freeSpinCount)
    {
        jackpots.gameObject.SetActive(false);
        ads.gameObject.SetActive(false);
        switch (freeSpinCount)
        {
            case 20:
                freeSpinTypeParent.SetActive(true);
                freeSpin20.SetActive(true);
                UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount = freeSpinCount;
                break;
            case 15:
                freeSpinTypeParent.SetActive(true);
                freeSpin15.SetActive(true);
                UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount = freeSpinCount;
                break;
            case 10:
                freeSpinTypeParent.SetActive(true);
                freeSpin10.SetActive(true);
                UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount = freeSpinCount;
                break;
            case 7:
                freeSpinTypeParent.SetActive(true);
                freeSpin7.SetActive(true);
                UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount = freeSpinCount;
                break;
            case 5:
                freeSpinTypeParent.SetActive(true);
                freeSpin5.SetActive(true);
                UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount = freeSpinCount;
                break;
            case -1:
                freeSpinTypeParent.SetActive(true);
                freeSpinMystery.SetActive(true);
                break;
        }
        wildMultiplier.SetActive(true);
        freeSpinCountObj.SetActive(true);

        freeSpinStart.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            freeSpinStart.SetActive(false);
            freeSpinParent.SetActive(false);
            freeSpinButton20.transform.DOKill();
            freeSpinButton20.transform.localScale = Vector3.one;
            freeSpinButton15.transform.DOKill();
            freeSpinButton15.transform.localScale = Vector3.one;
            freeSpinButton10.transform.DOKill();
            freeSpinButton10.transform.localScale = Vector3.one;
            freeSpinButton7.transform.DOKill();
            freeSpinButton7.transform.localScale = Vector3.one;
            freeSpinButton5.transform.DOKill();
            freeSpinButton5.transform.localScale = Vector3.one;
            freeSpinButtonMystery.transform.DOKill();
            freeSpinButtonMystery.transform.localScale = Vector3.one;
        });

        UltimateFireLinkChinaStreetFreeGameTransitionController.Instance.StartFreeSpins();
        UltimateFireLinkChinaStreetFreeGameTransitionController.Instance.UpdateFreeSpinsCount(UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinCount);
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class UltimateFireLinkChinaStreetPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[20]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 5]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 4; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class UltimateFireLinkChinaStreetPaylineEntry
{
    public UltimateFireLinkChinaStreetPaylineData payline;
    public int reelLimit;
    public string winText;
    public string symbol;
    public UltimateFireLinkChinaStreetPaylineEntry(UltimateFireLinkChinaStreetPaylineData payline, int reelLimit, string winText, string symbol)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.symbol = symbol;
        if (float.TryParse(winText, out float parsedValue))
        {
            this.winText = parsedValue.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class UltimateFireLinkChinaStreetPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;
    public string symbol;

    public UltimateFireLinkChinaStreetPaylineResult(int paylineNumber, int reelLimit, string winText, string symbol)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
        this.symbol = symbol;
    }
}

#endregion