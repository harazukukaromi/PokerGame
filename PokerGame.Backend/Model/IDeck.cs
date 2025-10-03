using System.Collections.Generic;

namespace PokerGame.Backend.Interfaces
{
    public interface IDeck
    {
        List<ICard> Cards { get; }
        int CardsRemaining { get; }
        void Initialize();
        void Shuffle(System.Random rng);
    }
}