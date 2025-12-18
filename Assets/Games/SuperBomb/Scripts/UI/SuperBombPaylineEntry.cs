public class SuperBombPaylineEntry
{
    public SuperBombPaylineData payline;
    public int reelLimit;
    public bool isLeftToRight;
    public float paylineWinAmount;

    public SuperBombPaylineEntry(SuperBombPaylineData payline, int reelLimit, float paylineWinAmount, bool isLeftToRight)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.paylineWinAmount = paylineWinAmount;
        this.isLeftToRight = isLeftToRight;
    }
}