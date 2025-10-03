namespace PokerGame;
public interface ITable
{
    List<IPlayer> players { get; }
    IDeck Deck { get; }
    List<Chip> Pot { get; }
}


