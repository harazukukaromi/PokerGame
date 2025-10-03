using System;
using PokerGame.Backend.Models;
using PokerGame.Backend.Game;
using PokerGame.Frontend;

// Alias supaya tidak bentrok namespace
using PGGame = PokerGame.Backend.Game.PokerGame;

class Program
{
    static void Main()
    {
        var deck = new Deck();
        deck.Initialize();
        deck.Shuffle(new Random());

        var table = new Table(deck);
        var game = new PGGame(table);  // pakai alias
        var ui = new ConsoleUI(game);

        ui.Run();
    }
}



