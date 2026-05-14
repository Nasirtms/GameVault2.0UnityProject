using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public class GameBetServices : MonoBehaviour
{
    public static GameBetServices Instance;

    public MonoBehaviour CurrentUIManager; 
    public TMP_Text CurrentCoinsText;     
    public System.Action UpdateCoinsAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public bool TrySpinWithCurrentBet(float betAmount)
    {
        float coins = UserManager.Instance.Coins;

        if (coins < betAmount)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Insufficient balance!");
            return false;
        }
        UserManager.Instance.currentBetAmount = betAmount;

        //float currentCoins = SafeParseCoins(CurrentCoinsText.text.ToString());
        //Debug.Log("Coins : " + coins);
        //Debug.Log("currentCoins : " + currentCoins);
        float updateCoin = coins - betAmount;
        //Debug.Log("Update Coin : " + updateCoin);

        UserManager.Instance.Coins = updateCoin;

        UpdateCoinsAction?.Invoke();
        return true;
    }

    #region Play Win Animation

    float _winAmount;
    public void PlayWinAnimation(float betAmount, float winAmount, float newBalance)
    {
        if (CurrentUIManager == null)
        {
            Debug.LogError("❌ CurrentUIManager is NULL!");
            return;
        }

        _winAmount = winAmount;
        string method = null;

        if (winAmount >= betAmount * 5000)
            method = "PlayJackpotWinAnimation";
        else if (winAmount >= betAmount * 500)
            method = "PlaySuperWinAnimation";
        else if (winAmount >= betAmount * 100)
            method = "PlayMegaWinAnimation";
        else if (winAmount >= betAmount * 50)
            method = "PlayBigWinAnimation";
        else if (winAmount >= betAmount * 10)
            method = "PlayNiceWinAnimation";
        else
            method = "UpdateWinAmount";

        CallAnimationMethod(method, winAmount);
        UpdateCoins(newBalance);
        Debug.Log("newBalance : " + newBalance);
    }


    private void CallAnimationMethod(string methodName, float amount)
    {
        var method = CurrentUIManager.GetType().GetMethod(methodName);

        if (method == null)
        {
            Debug.LogError("Method not found: " + methodName);
            return;
        }

        int paramCount = method.GetParameters().Length;

        if (paramCount == 1)
        {
            // e.g. PlayBigWinAnimation(float)
            method.Invoke(CurrentUIManager, new object[] { amount });
        }
        else if (paramCount == 2)
        {
            // e.g. UpdateWinAmount(float, bool)
            method.Invoke(CurrentUIManager, new object[] { amount, false });
        }
        else
        {
            method.Invoke(CurrentUIManager, null);
        }
    }


    #endregion


    public void UpdateCoins(float newAmount)
    {
        Debug.Log("Coins : " + newAmount);
        UserManager.Instance.UpdateCoins(newAmount);
        //CurrentCoinsText.text = newAmount.ToString();
        UpdateCoinsAction?.Invoke();
    }


    //private float SafeParseCoins(string value)
    //{
    //    if (string.IsNullOrWhiteSpace(value))
    //        return 0f;

    //    value = value.Replace(" ", "").Replace(",", "").ToUpper();

    //    float multiplier = 1f;

    //    if (value.EndsWith("K")) { multiplier = 1_000f; value = value[..^1]; }
    //    else if (value.EndsWith("M")) { multiplier = 1_000_000f; value = value[..^1]; }
    //    else if (value.EndsWith("B")) { multiplier = 1_000_000_000f; value = value[..^1]; }

    //    return float.TryParse(value, out float number) ? number * multiplier : 0f;
    //}


    // <-- THIS METHOD SETS EVERYTHING (1 line per UI)
    public void SetActiveUI(MonoBehaviour ui, TMP_Text coinsText,System.Action updateCoins)
    {
        CurrentUIManager = ui;
        CurrentCoinsText = coinsText;
        UpdateCoinsAction = updateCoins;
    }
}
