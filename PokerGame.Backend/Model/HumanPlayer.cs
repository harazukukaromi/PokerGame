namespace PokerGame;
public class HumanPlayer : IPlayer
{
    public string Name { get; }
    public IHand Hand { get; } = new Hand();
    public List<Chip> Chips { get; } = new();
    public bool IsFolded { get; set; }
    public int CurrentBet { get; set; }
    public int Balance { get; set; }
    public int TotalContributed { get; set; }

    public HumanPlayer(string name, int initialChips = 1000)
    {
        Name = name;
        Balance = initialChips;
    }
}