namespace PokerGame;
public interface IPlayer
{
    string Name { get; }
    IHand Hand { get; }
    List<Chip> Chips { get; }
    bool IsFolded { get; set; }
    int CurrentBet { get; set; }
    int Balance { get; set; }
    int TotalContributed { get; set; }
}
