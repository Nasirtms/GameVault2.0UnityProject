[System.Serializable]
public class FruitParadisePaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public FruitParadisePaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}