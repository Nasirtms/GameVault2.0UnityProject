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
    public double prize;  
    public string timestamp;  
    public float totalWin;
    public int scatterCount;
    public int freeSpinCount;
    public int freeSpinMultiplier;
    public bool isTreasureChestTriggered;
    public float newBalance;
    public string cardName;
    public bool goldenDragonIsBonus;
    public int goldenDragonIsBonusMultiplier;
    public TreasureChestResult treasureChestResult;
    public bool isRandomRespinTriggered;
    public List<int> respinReels;
    public int StickinRichMultiplier;
    public int StickinRichcurrencyValue1;
    public int StickinRichcurrencyValue2;
    public LifeOfLuxuryFreeSpinState lifeOfLuxuryFreeSpinState;
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
    public bool bonusTriggered;
    public List<int> cashValue;
    public List<int> cashIndex;
}

//LifeOfLuxury Game
public class LifeOfLuxuryFreeSpinState
{
    public int remainingSpins;
    public int lineMultiplier;
}
//ZombieParadise SlotGame
[System.Serializable]
public class ZombieParadiseSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<ZombieParadisePaylineWin> paylineWins;
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

[System.Serializable]
public class GoldGobblersSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<GoldGobblersPaylineWin> paylineWins;
    public bool isFreeSpin;
}
[System.Serializable]
public class GoldGobblersPaylineWin
{
    public List<int> paylineIndex;
    public string symbol;
    public int count;
    public double winAmount;
    public int wildCount;
}

//CashVault SlotGame
[System.Serializable]
public class CashVaultSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<CashVaultPaylineWin> paylineWins;
}

[System.Serializable]
public class CashVaultPaylineWin
{
    public List<int> paylineIndex;
    public string symbol;
    public int count;
    public double winAmount;
    public int wildCount;
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
    public int symbolFreeSpinCount;
}

//Bigger Bass Bonanza
[System.Serializable]
public class BiggerBassBonanzaSpinResult : BaseSpinResult
{
    public List<List<BiggerBassBonanzaSymbolData>> reels;
    public List<PaylineWinData> paylineWins;
}

[System.Serializable]
public class BiggerBassBonanzaSymbolData : SymbolData
{
    public float fishAmount;
}

//Paylines Data in SlotMachine Games and BiggerBassBonanza
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

//GoldRushGus MiniGame
[System.Serializable]
public class TreasureChestResult
{
    public string type;                 
    public float amount;
    public int coinMultiplier;
}

#region UltimateFireLinkChinaStreetSpinResult

[System.Serializable]
public class UltimateFireLinkChinaStreetSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<UltimateFireLinkChinaStreetPaylineWin> paylineWins;
}

[System.Serializable]
public class UltimateFireLinkRiverWalkSpinResult : BaseSpinResult
{
    public List<List<SymbolData>> reels;
    public List<UltimateFireLinkRiverWalkPaylineWin> paylineWins;
}

[System.Serializable]
public class UltimateFireLinkChinaStreetPaylineWin
{
    public List<int> paylineIndex;
    public string symbol;
    public int count;
    public double winAmount;
    public int wildCount;
}

[System.Serializable]
public class UltimateFireLinkRiverWalkPaylineWin
{
    public List<int> paylineIndex;
    public string symbol;
    public int count;
    public double winAmount;
    public int wildCount;
}


#endregion