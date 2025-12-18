
using UnityEngine;
using UnityEngine.UI;
public enum RPSChoice { Rock = 0, Paper = 1, Scissors = 2 }
public class InputManager_RPS : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BetManager_RPS betManager;
    [SerializeField] private GameManager_RPS gameManager;
    [SerializeField] private UIManager_RPS uiManager;
    [SerializeField] private WheelManager_RPS wheelManager;
    [SerializeField] private ComputerChoiceManager_RPS compChoiceManager;


    public void OnIncreaseBet()
    {
        UIManager_RPS.Instance.PlaySound("Click");
        if (gameManager.IsBetLocked) return;

        betManager.IncreaseBet();
        uiManager.UpdateBet();

        float[] table = wheelManager.UpdateSegmentLabels(
            betManager.CurrentBet,
            gameManager.CurrentWheelLevel
        );
        float ruby = table[8] * betManager.CurrentBet;
        float diamond = table[0] * betManager.CurrentBet;
        uiManager.UpdateCurrencyDisplays(ruby, diamond);
        uiManager.UpdateSegmentTexts(betManager.CurrentBet, table);
    }

    public void OnDecreaseBet()
    {
        UIManager_RPS.Instance.PlaySound("Click");
        if (gameManager.IsBetLocked) return;
        betManager.DecreaseBet();
        uiManager.UpdateBet();

        float[] table = wheelManager.UpdateSegmentLabels(
            betManager.CurrentBet,
            gameManager.CurrentWheelLevel
        );
        uiManager.UpdateCurrencyDisplays(table[8] * betManager.CurrentBet, table[0] * betManager.CurrentBet);
        uiManager.UpdateSegmentTexts(betManager.CurrentBet, table);
    }

    public void OnPlay()
    {
        uiManager.UpdateBet();
        UIManager_RPS.Instance.PlaySound("Play");
        UIManager_RPS.Instance.PlaySound("AfterPlay");
        compChoiceManager.StartFastCycle();

        gameManager.OnPlayPressed();
        gameManager.ResetLevel();
        uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
        wheelManager.pointerPivot.localEulerAngles = Vector3.zero;
    }
    public void OnRock()
    {
        uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(true);
        uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
        UIManager_RPS.Instance.PlaySound("Click");
        UIManager_RPS.Instance.PlaySound("BeforeChoice");
        Backend_RPS.Instance.SendRPSChoice("rock");
        uiManager.SetChoiceInteractable(false);
    }
    public void OnPaper()
    {
        uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(true);
        uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);
        UIManager_RPS.Instance.PlaySound("Click");
        UIManager_RPS.Instance.PlaySound("BeforeChoice");
        Backend_RPS.Instance.SendRPSChoice("paper");
        uiManager.SetChoiceInteractable(false);
    }
    public void OnScissors()
    {
        uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(true);
        UIManager_RPS.Instance.PlaySound("Click");
        UIManager_RPS.Instance.PlaySound("BeforeChoice");
        Backend_RPS.Instance.SendRPSChoice("scissors");
        uiManager.SetChoiceInteractable(false);
    }
}
