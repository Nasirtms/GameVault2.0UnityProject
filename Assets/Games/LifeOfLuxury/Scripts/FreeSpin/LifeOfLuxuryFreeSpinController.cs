using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

public class LifeOfLuxuryFreeSpinController : MonoBehaviour
{
    #region Variables
    public static LifeOfLuxuryFreeSpinController Instance;

    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;
    private Coroutine freeSpinRoutine;
    private bool cancelRequested;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += CancelFreeSpins;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= CancelFreeSpins;
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    #endregion

    #region Public References

    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        cancelRequested = false;
        isFreeGame = true;
        firstSpin = true;
        freeSpinDone = 0;
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins += freeSpins;
        UpdateSpinCount();
    }
    public void InitialFreeSpinText()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = ToSpriteSpinCount(0, totalFreeSpins);
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    #endregion

    #region Free Spin

    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = ToSpriteSpinCount(freeSpinDone, totalFreeSpins);
    }
    private string ToSpriteDigits(int value)
    {
        string s = value.ToString();

        StringBuilder sb = new StringBuilder(s.Length * 10);

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (ch >= '0' && ch <= '9')
            {
                sb.Append($"<sprite index={ch - '0'}>");
            }
        }

        return sb.ToString();
    }

    private string ToSpriteSpinCount(int current, int total)
    {
        return $"{ToSpriteDigits(current)}\n\n{ToSpriteDigits(total)}";
    }
    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (freeSpinDone < totalFreeSpins)
        {
            if (firstSpin)
                firstSpin = false;
            else
                yield return new WaitForSeconds(delayBetweenSpins);

            if (cancelRequested) yield break;

            float betAmount = LifeOfLuxuryUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            freeSpinDone++;

            UpdateSpinCount();

            yield return new WaitUntil(() => LifeOfLuxurySlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;

            if (LifeOfLuxurySlotMachine.Instance.currentSpinResult != null)
            {
                if (LifeOfLuxurySlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted);
                }
            }
        }

        yield return new WaitForSeconds(1f);
        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
        isFreeGame = false;

        LifeOfLuxurySlotMachine.Instance.isFreeGame = false;

        LifeOfLuxuryFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    private void CancelFreeSpins()
    {
        if (!isFreeGame) return;

        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        LifeOfLuxuryUIManager.Instance.UpdateButtons("Free Spin End");
    }
    #endregion
}