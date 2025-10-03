namespace PokerGame.Backend.Interfaces
{
    public enum ChipType
    {
        White = 10,
        Red = 50,
        Green = 100,
        Black = 1000
    }

    public interface IChip
    {
        ChipType Type { get; }
    }
}