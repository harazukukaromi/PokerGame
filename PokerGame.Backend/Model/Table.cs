using System;
using System.Collections.Generic;
using PokerGame.Backend.Interfaces;

namespace PokerGame.Backend.Models
{
    public class Table : ITable
    {
        public List<IPlayer> players { get; } = new();
        public IDeck Deck { get; }
        public List<IChip> Pot { get; } = new List<IChip>();

        public Table(IDeck deck)
        {
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }
    }
}