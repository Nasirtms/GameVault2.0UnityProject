[System.Serializable]
public class RichLittlePiggiesPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public RichLittlePiggiesPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}