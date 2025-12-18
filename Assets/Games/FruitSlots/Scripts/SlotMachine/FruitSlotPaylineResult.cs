[System.Serializable]
public class FruitSlotPaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public FruitSlotPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}