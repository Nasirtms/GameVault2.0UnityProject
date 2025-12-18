[System.Serializable]
public class StarBurstSlotsPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public float paylineWinAmount;
    public bool isleftToRight;

    public StarBurstSlotsPaylineResult(int paylineNumber, int reelLimit, float paylineWinAmount, bool isleftToRight)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.paylineWinAmount = paylineWinAmount;
        this.isleftToRight = isleftToRight;
    }
}