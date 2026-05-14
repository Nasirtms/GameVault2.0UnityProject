public class RichLittlePiggiesPaylineEntry
{
    public RichLittlePiggiesPaylineData payline;
    public int reelLimit;
    public string winText;

    public RichLittlePiggiesPaylineEntry(RichLittlePiggiesPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}