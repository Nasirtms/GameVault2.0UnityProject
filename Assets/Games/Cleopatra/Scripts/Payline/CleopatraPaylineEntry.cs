public class CleopatraPaylineEntry
{
    public CleopatraPaylineData payline;
    public int reelLimit;
    public string winText;

    public CleopatraPaylineEntry(CleopatraPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}