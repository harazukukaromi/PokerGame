namespace PokerGame;
public interface IDeck
{
    List<ICard> Cards { get; }
    int CardsRemaining { get; }
    void Initialize();
    void Shuffle(Random rng);
}