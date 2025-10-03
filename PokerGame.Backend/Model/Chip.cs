using PokerGame.Backend.Interfaces;

namespace PokerGame.Backend.Models
{
    public class Chip : IChip
    {
        public ChipType Type { get; }
        public Chip(ChipType type) => Type = type;
    }
}