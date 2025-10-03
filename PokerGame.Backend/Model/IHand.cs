using System.Collections.Generic;

namespace PokerGame.Backend.Interfaces
{
    public interface IHand
    {
        List<ICard> Cards { get; }
    }
}