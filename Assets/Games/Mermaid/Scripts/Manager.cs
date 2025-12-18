using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    public static Manager Instance { get; private set; }

    [Header("UI REFERENCES")]
    [SerializeField] private GameObject menuPanel;
    //[SerializeField] private GameObject settingsPanel;
    //[SerializeField] public GameObject fishesPanel;
    [SerializeField] public Button ruleForwardButton;
    [SerializeField] public Button ruleBackwardButton;
    [SerializeField] public GameObject[] rulePages;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button menuOpenButton;
    [SerializeField] private Button menuCloseButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button musicToggle;
    [SerializeField] private Button soundToggle;
    [SerializeField] private Text betText;
    public GameObject playerUI;
    public GameObject wave;
    [SerializeField] private MainMenu.UILoadingPanel loadingPanel;

    [Header("Balance")]
    [Tooltip("UI Text to show current balance")]
    [SerializeField] private Text balanceText;
    [Tooltip("Starting money for the player")]
    [SerializeField] private float startingBalance = 100f;
    [HideInInspector] public float balance;
    [SerializeField] private Transform wallsParent;

    private Coroutine lockCoroutine;
    public float[] betOptions = { 0.1f, 0.2f, 0.3f, 0.5f, 0.9f, 1f, 2f, 3f, 4f, 5f, 10f };
    [HideInInspector] public int betIndex = 0;
    [HideInInspector] public Camera mainCam;
    private float minX, maxX, minY, maxY;

    private FishManager fishManager;

    public static float currentBetAmoun = 0f;
    public static float totalFishWinAmountPerInterval = 0f;
    public static float totalBetAmountPerInterval = 0f;
    public static double targetRTBFromBackend = 0f;
    public static Action onHealthMultiplier;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        mainCam = Camera.main;
        ComputeScreenBounds();
        DOTween.Init();
        fishManager = GetComponent<FishManager>();

    }
    private void Start()
    {
        wave.GetComponent<Animator>().Play("wave");
        //PositionWalls();
        if (increaseBetButton != null) increaseBetButton.onClick.AddListener(OnIncreaseBet);
        if (decreaseBetButton != null) decreaseBetButton.onClick.AddListener(OnDecreaseBet);
        if (menuOpenButton != null) menuOpenButton.onClick.AddListener(OnMenuOpen);
        if (menuCloseButton != null) menuCloseButton.onClick.AddListener(OnMenuClose);
        if (ruleForwardButton != null) ruleForwardButton.onClick.AddListener(RuleForward);
        if (ruleBackwardButton != null) ruleBackwardButton.onClick.AddListener(RuleBackward);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitGame);
        UpdateBetUI();

        startingBalance = UserManager.Instance.Coins;
        balance = startingBalance;
        UpdateBalanceUI();
        totalFishWinAmountPerInterval = 0.0f;
        totalBetAmountPerInterval = 0.0f;

        Invoke(nameof(MoveFishForwardAndCloseLoading), 1);
        StartCoroutine(nameof(MoveFishForwardAndCloseLoading));
    }

    IEnumerator MoveFishForwardAndCloseLoading()
    {
        yield return new WaitUntil(() => FishManager.Instance.ActiveFishCount > 0);
        yield return new WaitForSeconds(4);

        FishManager.Instance.MoveAllFishForward(60 * 10);
        loadingPanel.ClosePanel(.5f);
    }

    //public void CloseFishesPanel()
    //{
    //        fishesPanel.SetActive(false);
    //}

    public void UpdateBalanceUI()
    {
        if (balanceText != null)
        {
            float truncated = Mathf.Floor(balance * 10f) / 10f;
            balanceText.text = truncated.ToString("F1");
        }
    }
    public void RuleForward()
    {
        if (rulePages[0].activeSelf)
        {
            rulePages[0].SetActive(false);
            rulePages[1].SetActive(true);
        }
        else if (rulePages[1].activeSelf)
        {
            rulePages[1].SetActive(false);
            rulePages[0].SetActive(true);
        }
    }
    public void RuleBackward()
    {
        if (rulePages[0].activeSelf)
        {
            rulePages[0].SetActive(false);
            rulePages[1].SetActive(true);
        }
        else if (rulePages[1].activeSelf)
        {
            rulePages[1].SetActive(false);
            rulePages[0].SetActive(true);
        }
    }

    private Vector3 originalLocalPos;

    public void OnIncreaseBet()
    {
        betIndex = (betIndex + 1) % betOptions.Length; UpdateBetUI(); 
        GunManager.Instance.UpdateGun(betIndex);
        previousBet = betIndex;
    }
    public void OnDecreaseBet() 
    { 
        betIndex = (betIndex - 1 + betOptions.Length) % betOptions.Length; UpdateBetUI(); 
        GunManager.Instance.UpdateGun(betIndex); 
    }

    private void UpdateBetUI()
    {
        if (betText != null)
            betText.text = $"{betOptions[betIndex]}";
        currentBetAmoun = betOptions[betIndex];
    }
    float previousBet;

    public void OnMenuOpen() { if (menuPanel != null) menuPanel.SetActive(true); }
    public void OnMenuClose() { if (menuPanel != null) menuPanel.SetActive(false); }
    public void OnQuitGame()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManager.LoadScene("Main");
    }
    private void ComputeScreenBounds()
    {
        float zDist = -mainCam.transform.position.z;
        Vector3 bl = mainCam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        Vector3 tr = mainCam.ViewportToWorldPoint(new Vector3(1, 1, zDist));
        minX = bl.x; minY = bl.y;
        maxX = tr.x; maxY = tr.y;
    }
 
    private void PositionWalls()
    {
        float zDist = -mainCam.transform.position.z;
        Vector3 bl = mainCam.ViewportToWorldPoint(new Vector3(0, 0, zDist));
        Vector3 tr = mainCam.ViewportToWorldPoint(new Vector3(1, 1, zDist));
        float minX = bl.x, minY = bl.y, maxX = tr.x, maxY = tr.y;
        float thickness = 0.5f;

        void SetupWall(string name, Vector2 size, Vector3 pos)
        {
            Transform w = wallsParent.Find(name);
            if (w == null) return;
            var bc = w.GetComponent<BoxCollider2D>();
            bc.size = size;
            w.position = pos;
        }

        SetupWall("LeftWall",
            new Vector2(thickness, (maxY - minY) + 2f),
            new Vector3(minX - (thickness / 2), (minY + maxY) / 2, 0)
        );
        SetupWall("RightWall",
            new Vector2(thickness, (maxY - minY) + 2f),
            new Vector3(maxX + (thickness / 2), (minY + maxY) / 2, 0)
        );
        SetupWall("BottomWall",
            new Vector2((maxX - minX) + 2f, thickness),
            new Vector3((minX + maxX) / 2, minY - (thickness / 2), 0)
        );
        SetupWall("TopWall",
            new Vector2((maxX - minX) + 2f, thickness),
            new Vector3((minX + maxX) / 2, maxY + (thickness / 2), 0)
        );
    }
}