using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FruitMaryFruitMaryGameManager : MonoBehaviour
{
    #region Variables

    public static FruitMaryFruitMaryGameManager Instance;

    [Header("Popup")]
    [SerializeField] private GameObject fruitMaryPopup;

    [Header("Fruit Mary")]
    [SerializeField] private GameObject fruitMaryGame;
    [SerializeField] private TMP_Text spinCount;
    [SerializeField] private TMP_Text totalCoins;
    [SerializeField] private TMP_Text winAmount;
    [SerializeField] private TMP_Text betAmount;
    private int freeSpins;

    [Header("Fruit Mary Rules")]
    [SerializeField] private GameObject fruitMaryRulesPopup;
    [SerializeField] private Button continueButton;
    private bool startGame;

    [Header("Fruit Mary End")]
    [SerializeField] private GameObject fruitMaryEndPopup;
    [SerializeField] private TMP_Text mainGameWinText;
    [SerializeField] private TMP_Text littleMaryGameWinText;
    [SerializeField] private Button exitButton;
    private bool endGame;

    private float mainGameWin;
    private float mainGameBetAmount;
    private float littleMaryGameWin;

    public Coroutine textAnimationCoroutine;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        UserManager.Instance.UpdateGameCoins += UpdateCoins;

        endGame = false;
        startGame = false;

        continueButton.onClick.AddListener(() => startGame = true);
        exitButton.onClick.AddListener(() => endGame = true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Fake Fruit Mary Game

    [ContextMenu("Start Fake Game`")]
    public void StartFakeMaryGame(int count, float betAmount, float winAmount)
    {
        UpdateCoins();
        UpdateMainGameWin(winAmount);
        UpdateBetAmount(betAmount);
        UpdateWinAmount(0f);
        UpdateSpinCount(count);
        StartFruitMaryGame();
    }

    #endregion

    #region UI Update

    private void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            totalCoins.text = $"Chips: {UserManager.Instance.FormatCoins(UserManager.Instance.Coins)}";
        }
    }

    private void UpdateBetDisplay()
    {
        betAmount.text = $"Bet: {mainGameBetAmount.ToString("F2")}";
    }

    private void UpdateFreeSpins(int count)
    {
        spinCount.text = $"Spins: {count.ToString()}";
    }

    public void UpdateWinAmount(float winAmount)
    {
        littleMaryGameWin += winAmount;
        PlayTextAnimation(littleMaryGameWin);
    }

    #endregion

    #region Game Transition

    public void StartFruitMaryGame()
    {
        endGame = false;
        startGame = false;
        littleMaryGameWin = 0;

        StartCoroutine(StartFruitMaryGameTransition());
    }

    public void EndFruitMaryGame()
    {
        StartCoroutine(EndFruitMaryGameTransition());
    }

    private IEnumerator StartFruitMaryGameTransition()
    {
        startGame = false;

        PopupAnimation(fruitMaryPopup, 1f, 1f, true);

        yield return new WaitForSeconds(2.5f);

        PopupAnimation(fruitMaryPopup, 0f, 1f, false);

        yield return new WaitForSeconds(0.5f);

        fruitMaryGame.SetActive(true);

        PopupAnimation(fruitMaryRulesPopup, 1.5f, 1f, true);

        yield return new WaitUntil(() => startGame);

        PopupAnimation(fruitMaryRulesPopup, 0f, 1f, false);

        yield return new WaitForSeconds(1f);

        FruitMaryFruitMaryGameSpinService.Instance.Spin(mainGameBetAmount);
    }

    private IEnumerator EndFruitMaryGameTransition()
    {
        mainGameWinText.text = mainGameWin.ToString("F2");
        littleMaryGameWinText.text = littleMaryGameWin.ToString("F2");

        PopupAnimation(fruitMaryEndPopup, 1.5f, 1f, true);

        yield return new WaitUntil(() => endGame);
        
        PopupAnimation(fruitMaryEndPopup, 0f, 1f, false);

        fruitMaryGame.SetActive(false);
        FruitMarySlotMachine.Instance.isBonusGameCompleted = true;
        if (FruitMarySlotMachine.Instance.isFreeGame)
        {
            FruitMaryUIManager.Instance.UpdateButtons("Free Spin");
        }
        else
        {
            FruitMaryUIManager.Instance.UpdateButtons("Transition End");
        }
            
        FruitMarySlotMachine.Instance.isFruitMaryGameReady = false;
    }

    #endregion

    #region Text Animation

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
    }

    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate)
    {
        float startValue = 0f;

        if (!string.IsNullOrEmpty(textToAnimate.text) && float.TryParse(textToAnimate.text, out float current))
        {
            startValue = current;
        }
        //Debug.Log($"start value, {startValue:0.00}");
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, target, t);
            textToAnimate.text = $"Win: {displayed.ToString("0.00")}";

            timer += Time.deltaTime;
            yield return null;
        }
        //Debug.Log($"animate to target value, {target:0.00}");
        textToAnimate.text = $"Win {target.ToString("0.00")}";
    }

    #endregion

    #region Helper Functions

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    public void UpdateMainGameWin(float winAmount)
    {
        mainGameWin = winAmount;
    }

    public void UpdateBetAmount(float betAmount)
    {
        mainGameBetAmount = betAmount;
        UpdateBetDisplay();
    }

    public void UpdateSpinCount(int spinCount)
    {
        freeSpins = spinCount;
        UpdateFreeSpins(freeSpins);
    }

    public void DcreaseFreeSpinCount()
    {
        freeSpins--;
        UpdateFreeSpins(freeSpins);
    }

    public int GetFreeSpinCount()
    {
        return freeSpins;
    }

    public float GetBetAmount()
    {
        return mainGameBetAmount;
    }

    #endregion
}
