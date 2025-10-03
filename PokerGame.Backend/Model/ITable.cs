using System.Collections.Generic;

namespace PokerGame.Backend.Interfaces
{
    public interface ITable
    {
        List<IPlayer> players { get; }
        IDeck Deck { get; }
        List<IChip> Pot { get; }   //pakai interface IChip, bukan Chip langsung
    }
}