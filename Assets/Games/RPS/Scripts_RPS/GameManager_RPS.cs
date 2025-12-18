// GameManager.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;



public class GameManager_RPS : MonoBehaviour
{
    [Header("Forced Controls")]
    [SerializeField] public bool forceWin;
    [SerializeField] public bool forceLose;
    [SerializeField] public bool forceTie;
    [SerializeField] public int chosen;

    [Header("Managers & UI")]
    [SerializeField] private BetManager_RPS betManager;
    [SerializeField] private WheelManager_RPS wheelManager;
    [SerializeField] private UIManager_RPS uiManager;
    [SerializeField] private ComputerChoiceManager_RPS compChoiceManager;
    [SerializeField] private Backend_RPS backendManager;
    [SerializeField] public Image ligthOn;
    [SerializeField] public Image ligthOff;
    [SerializeField] public GameObject coins;

    public float balance;
    private int tieCount = 0;
    private int wheelLevel = 1;

    // Expose current level for others
    public int CurrentWheelLevel => wheelLevel;
    public bool IsBetLocked { get; private set; }
    public void LockBet() => IsBetLocked = true;
    public void UnlockBet() => IsBetLocked = false;

    private void Update()
    {
        if (forceWin)
        {
            forceTie = false;
            forceLose = false;
        }
        else if (forceLose)
        {
            forceTie = false;
            forceWin = false;
        }
        else if (forceTie)
        {
            forceWin = false;
            forceLose = false;
        }
        uiManager.UpdateBet();
        uiManager.UpdateCoins();
    }

    private void Start()
    {
        UpdateSlotServicesGameName();
        ligthOn.enabled = false;
        ligthOff.enabled = true;
        wheelManager.StartCoroutine(LightFlickerCoroutine(ligthOn, ligthOff, 0.5f));
        balance = UserManager.Instance.Coins;
        uiManager.UpdateCoins();
        uiManager.UpdateBet();
        uiManager.UpdateWheelLevel(wheelLevel);
        uiManager.SetChoiceInteractable(false);
        uiManager.SetMessage("Select Bet, Then Press Play To Start");
        uiManager.UpdateWheelLevel(wheelLevel);

        float initBet = betManager.CurrentBet;
        uiManager.UpdateBet();
        float[] table = wheelManager.UpdateSegmentLabels(initBet, wheelLevel);
        uiManager.UpdateCurrencyDisplays(
        table[8] * initBet,
        table[0] * initBet
        );
        uiManager.UpdateSegmentTexts(initBet, table);
        coins.gameObject.SetActive(false);
    }
    #region Machine Registery


    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }
    IEnumerator LightFlickerCoroutine(Image lighton, Image lightoff, float interval)
    {
        while (true)
        {
            // Turn ON light
            lighton.enabled = true;
            lightoff.enabled = false;
            yield return new WaitForSeconds(interval);

            // Turn OFF light
            lighton.enabled = false;
            lightoff.enabled = true;
            yield return new WaitForSeconds(interval);
        }
    }

    #endregion
    public void OnPlayPressed()
    {
        var bet = betManager.CurrentBet;
        if (balance < bet)
        {
            uiManager.SetMessage("Insufficient Balance to play!");
            compChoiceManager.StopCycle();
            return;
        }
        balance -= bet;

        GameBetServices.Instance.TrySpinWithCurrentBet(bet);

        //uiManager.UpdateBalance();
        uiManager.ResetWinningAmount();
        LockBet();
        uiManager.SetPlayInteractable(false);
        uiManager.SetChoiceInteractable(true);
        uiManager.SetMessage("Choose Rock, Paper, or Scissors");
        coins.gameObject.SetActive(false);
    }

    // GameManager.cs
    public void OnPlayerChoice(string res)
    {

        if (res.Equals("win"))
        {
            UnlockBet();
            HandleWin();
        }
        else if (res.Equals("draw"))
        {
            LockBet();
            HandleTie();
        }
        else if(res.Equals("lose"))
        {

            UnlockBet();
            HandleLose();
        }
    }

    private void HandleWin()
    {
        UIManager_RPS.Instance.PlaySound("Win");
        uiManager.SetChoiceInteractable(false);
        //uiManager.SetMessage("You Win! Spinning wheel...");

        StartCoroutine(wheelManager.SpinWheel(wheelLevel, _ =>
        {
            float winAmount = backendManager._current_RPSResponse.winAmount;
            uiManager.SetPlayInteractable(false);
            uiManager.AnimateWinning(winAmount, () =>
            {
                if (winAmount > 0f)
                {
                    balance += winAmount;
                    coins.gameObject.SetActive(true);
                    var ps = coins.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        StartCoroutine(PlayCoinWinMusicWhileParticles(ps.main.duration));
                    }
                }
                Invoke(nameof(UpdateGameCoin), 1f);
                uiManager.SetMessage($"You won {winAmount:F2}!");
                uiManager.SetPlayInteractable(true);
                uiManager.playButton.image.sprite = uiManager.playButtonSprite1;
                if (uiManager.rockButton.transform.GetChild(1).gameObject.activeSelf || uiManager.paperButton.transform.GetChild(1).gameObject.activeSelf || uiManager.scissorsButton.transform.GetChild(1).gameObject.activeSelf)
                {
                    uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
                    uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
                    uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
                }
            });
        }));

    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(backendManager._current_RPSResponse.newBalance);
    }

    private IEnumerator PlayCoinWinMusicWhileParticles(float duration)
    {
        UIManager_RPS.Instance.PlayWinMusic("Coin Win"); // start looping
        yield return new WaitForSeconds(duration);    // wait until particles are done
        UIManager_RPS.Instance.StopWinMusic("Coin Win");           // stop looping
    }

    private void HandleLose()
    {
        Invoke(nameof(UpdateGameCoin), 1f);
        UIManager_RPS.Instance.PlaySound("Lose");
        uiManager.SetChoiceInteractable(false);
        uiManager.SetMessage($"You Lost {betManager.CurrentBet:F2}.");
        uiManager.SetPlayInteractable(true);
        uiManager.playButton.image.sprite = uiManager.playButtonSprite1;

        if (uiManager.rockButton.transform.GetChild(1).gameObject.activeSelf || uiManager.paperButton.transform.GetChild(1).gameObject.activeSelf || uiManager.scissorsButton.transform.GetChild(1).gameObject.activeSelf)
        {
            uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
            uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
            uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    private void HandleTie()
    {
        UIManager_RPS.Instance.PlaySound("Tie");
        tieCount++;
        if (tieCount <= 5)
        {
            wheelManager.StartCoroutine(wheelManager.FadeGlow(0.5f, 0.2f));
        }
        wheelLevel = Mathf.Min(1 + tieCount, 5);
        float[] table = wheelManager.UpdateSegmentLabels(betManager.CurrentBet, wheelLevel);
        uiManager.UpdateWheelLevel(wheelLevel);
        uiManager.UpdateCurrencyDisplays(
            table[8] * betManager.CurrentBet,
            table[0] * betManager.CurrentBet
        );
        uiManager.SetMessage("Tie! Choose Rock, Paper, or Scissor");
        uiManager.UpdateSegmentTexts(betManager.CurrentBet, table);
        uiManager.SetPlayInteractable(false);
        uiManager.SetChoiceInteractable(true);

        if (uiManager.rockButton.transform.GetChild(1).gameObject.activeSelf || uiManager.paperButton.transform.GetChild(1).gameObject.activeSelf || uiManager.scissorsButton.transform.GetChild(1).gameObject.activeSelf)
        {
            uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
            uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
            uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
        }
        Invoke(nameof(UpdateGameCoin), 1f);
        uiManager.SetMessage("Make a New Choice");
    }

    public void ResetLevel()
    {
        tieCount = 0;
        wheelLevel = 1;
        float[] table = wheelManager.UpdateSegmentLabels(betManager.CurrentBet, wheelLevel);
        uiManager.UpdateWheelLevel(wheelLevel);
        uiManager.UpdateCurrencyDisplays(
            table[8] * betManager.CurrentBet,
            table[0] * betManager.CurrentBet
        );
        uiManager.UpdateSegmentTexts(betManager.CurrentBet, table);
    }

    public void DetermineComputerChoice(RPSChoice playerChoice, out RPSChoice computerChoice)
    {
        if (forceWin)
        {
            computerChoice = GetLosingChoice(playerChoice);
        }
        else if (forceLose)
        {
            computerChoice = GetWinningChoice(playerChoice);
        }
        else if (forceTie)
        {
            computerChoice = playerChoice;
        }
        else
        {
            computerChoice = playerChoice;
        }
    }
    private RPSChoice GetWinningChoice(RPSChoice choice)
    {
        switch (choice)
        {
            case RPSChoice.Rock: return RPSChoice.Paper;
            case RPSChoice.Paper: return RPSChoice.Scissors;
            case RPSChoice.Scissors: return RPSChoice.Rock;
            default: return RPSChoice.Rock;
        }
    }

    private RPSChoice GetLosingChoice(RPSChoice choice)
    {
        switch (choice)
        {
            case RPSChoice.Rock: return RPSChoice.Scissors;
            case RPSChoice.Paper: return RPSChoice.Rock;
            case RPSChoice.Scissors: return RPSChoice.Paper;
            default: return RPSChoice.Rock;
        }
    }

}
