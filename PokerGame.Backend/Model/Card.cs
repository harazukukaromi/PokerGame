namespace PokerGame;

public class Card : ICard
{
    public Suit Suit { get; }
    public Rank Rank { get; }
    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }
}