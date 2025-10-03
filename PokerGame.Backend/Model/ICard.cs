namespace PokerGame.Backend.Interfaces
{
    public enum Rank
    {
        Two = 2, Three, Four, Five, Six, Seven,
        Eight, Nine, Ten, Jack, Queen, King, Ace
    }

    public enum Suit { Hearts, Diamonds, Clubs, Spades }

    public interface ICard
    {
        Suit Suit { get; }
        Rank Rank { get; }
    }
}