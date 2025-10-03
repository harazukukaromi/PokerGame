namespace PokerGame;

public class Chip : IChip
{
    public ChipType Type { get; }
    public Chip(ChipType type) => Type = type;
}