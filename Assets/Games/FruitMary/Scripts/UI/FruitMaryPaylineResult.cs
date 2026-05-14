[System.Serializable]
public class FruitMaryPaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public FruitMaryPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}