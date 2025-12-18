[System.Serializable]
public class CleopatraPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public CleopatraPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}