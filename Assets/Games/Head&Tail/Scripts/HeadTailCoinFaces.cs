// Assets/Scripts/Core/CoinFaces.cs
namespace HeadTailGame
{
    public static class HeadTailCoinFaces
    {
        public const int Heads = 0;
        public const int Tails = 1;

        public static bool IsValid(int v) => v == Heads || v == Tails;
        public static string ToText(int v) => v == Heads ? "Heads" : "Tails";
    }
}
