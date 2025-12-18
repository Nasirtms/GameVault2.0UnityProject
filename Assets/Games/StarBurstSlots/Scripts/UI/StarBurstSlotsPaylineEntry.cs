public class StarBurstSlotsPaylineEntry
{
    public StarBurstSlotsPaylineData payline;
    public int reelLimit;
    public bool isLeftToRight;
    public float paylineWinAmount;

    public StarBurstSlotsPaylineEntry(StarBurstSlotsPaylineData payline, int reelLimit, float paylineWinAmount, bool isLeftToRight)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.paylineWinAmount = paylineWinAmount;
        this.isLeftToRight = isLeftToRight;
    }
}