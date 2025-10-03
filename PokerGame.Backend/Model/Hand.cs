using System.Collections.Generic;
using PokerGame.Backend.Interfaces;

namespace PokerGame.Backend.Models
{
    public class Hand : IHand
    {
        private List<ICard> _cards = new();
        public List<ICard> Cards => _cards;
        public void AddCard(ICard card) => _cards.Add(card);
    }
}