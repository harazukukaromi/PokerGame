using System;
using System.Collections.Generic;
using PokerGame.Backend.Interfaces;

namespace PokerGame.Backend.Models
{
    public class Deck : IDeck
    {
        private List<ICard> _cards = new();
        public List<ICard> Cards => _cards;
        public int CardsRemaining => _cards.Count;

        public void Initialize()
        {
            _cards.Clear();
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
                foreach (Rank r in Enum.GetValues(typeof(Rank)))
                    _cards.Add(new Card(s, r));
        }

        public void Shuffle(Random rng)
        {
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (_cards[k], _cards[n]) = (_cards[n], _cards[k]);
            }
        }
    }
}