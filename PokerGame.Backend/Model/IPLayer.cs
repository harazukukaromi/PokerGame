using System.Collections.Generic;

namespace PokerGame.Backend.Interfaces
{
    public interface IPlayer
    {
        string Name { get; }
        IHand Hand { get; }
        List<IChip> Chips { get; }   // ganti Chip -> IChip
        bool IsFolded { get; set; }
        int CurrentBet { get; set; }
        int Balance { get; set; }
        int TotalContributed { get; set; }
    }
}
