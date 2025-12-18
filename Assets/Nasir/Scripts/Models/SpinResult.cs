using System;
using System.Collections.Generic;

[System.Serializable]
public abstract class BaseSpinResult
{
    public bool success;
    public string requestId;
    public string spinId;
    public string userId;
    public string gameId;
    public double prize;   // use double (superset of float)
    public string timestamp;  // keep as string for both (parse later if needed)
    public float totalWin;
    public int scatterCount;
    public int freeSpinCount;
    public float newBalance;
    public string cardName;
    public bool goldenDragonIsBonus;
    public int goldenDragonIsBonusMultiplier;
}

[System.Serializable]
public class SpinResult : BaseSpinResult
{
    public int forcedPrizeIndex;
    public List<List<SymbolData>> reels;
    public List<PaylineWinData> paylineWins;
    public List<BonusWinData> bonusWins;
    public JackpotWinData jackpotWin;
    public bool isFreeSpin;
    public bool isBonusGame;
    public float TotalBalance;
}

[System.Serializable]
public class BiggerBassBonanzaSpinResult : BaseSpinResult
{
    public List<List<BiggerBassBonanzaSymbolData>> reels;
    public List<PaylineWinData> paylineWins;
}

[System.Serializable]
public class ZombieParadiseSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<ZombieParadisePaylineWin> paylineWins;
}

[System.Serializable]
public class SymbolData
{
    public string id;
    public Dictionary<string, float> paytable;
    public float weight;
    public bool isBonus;
    public int bonusTriggerCount;
    public float bonusPayout;
    public bool showBorder;
    public bool scatter;
    public List<int> paylineNumbers;
}

[System.Serializable]
public class BiggerBassBonanzaSymbolData : SymbolData
{
    public float fishAmount;
}


[System.Serializable]
public class PaylineWinData
{
    public int paylineIndex;
    public string symbol;
    public int count;
    public string winAmount;
    public int wildCount;
    public bool IsLeft;
}

[System.Serializable]
public class BonusWinData
{
    public string symbolId;
    public int matchCount;
    public float payout;
}

[System.Serializable]
public class JackpotWinData
{
    public string type;
    public float amount;
    public bool hasValue;
}

[System.Serializable]
public class ZombieParadisePaylineWin
{
    public List<int> paylineIndex;
    public string symbol;
    public int count;
    public double winAmount;
    public int wildCount;
}