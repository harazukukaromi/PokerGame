using System;
using System.Linq;
using PokerGame.Backend.Game;
using PokerGame.Backend.Interfaces;
using PokerGame.Backend.Models;

// Alias biar class PokerGame tidak bentrok dengan namespace root
using PGGame = PokerGame.Backend.Game.PokerGame;

namespace PokerGame.Frontend
{
    public class ConsoleUI
    {
        private readonly PGGame _game;

        public ConsoleUI(PGGame game)
        {
            _game = game;

            // Binding semua event dari backend
            _game.OnGameEvent += HandleGameEvent;

            // Callback untuk keputusan pemain (Human)
            _game.OnPlayerDecision = AskPlayerAction;
        }

        private void HandleGameEvent(GameEventType evt, IPlayer? player)
        {
            switch (evt)
            {
                case GameEventType.GameStarted:
                    Console.WriteLine("=== Texas Hold'em Poker ===");
                    Console.WriteLine("Game dimulai!");
                    break;

                case GameEventType.RoundStart:
                    Console.WriteLine("\n--- Ronde Baru Dimulai ---");
                    break;

                case GameEventType.RoundEnded:
                    Console.WriteLine("\n=== Ronde Selesai ===");
                    break;

                case GameEventType.CardsDealt:
                    if (player is HumanPlayer)
                    {
                        string cards = string.Join(", ", player.Hand.Cards.Select(c => $"{c.Rank} of {c.Suit}"));
                        Console.WriteLine($"{player.Name} gets: {cards}");
                    }
                    else
                    {
                        Console.WriteLine($"{player?.Name} gets: [Hidden]");
                    }
                    break;

                case GameEventType.CommunityDealt:
                    Console.WriteLine("Community Cards:");
                    foreach (var c in _game.GetCommunityCards())
                        Console.WriteLine($"  {c.Rank} of {c.Suit}");
                    break;

                case GameEventType.PlayerFolded:
                    Console.WriteLine($"{player?.Name} Folded.");
                    break;

                case GameEventType.PlayerChecked:
                    Console.WriteLine($"{player?.Name} Checked.");
                    break;

                case GameEventType.PlayerCalled:
                    Console.WriteLine($"{player?.Name} Called.");
                    break;

                case GameEventType.PlayerRaised:
                    Console.WriteLine($"{player?.Name} Raised!");
                    break;

                case GameEventType.PlayerAllin:
                    Console.WriteLine($"{player?.Name} goes ALL-IN!");
                    break;

                case GameEventType.Showdown:
                    Console.WriteLine("\n--- Showdown ---");
                    foreach (var p in _game.GetPlayers().Where(pl => !pl.IsFolded))
                    {
                        var result = HandEvaluator.EvaluateHand(p.Hand.Cards.ToList(), _game.GetCommunityCards());
                        string hole = string.Join(", ", p.Hand.Cards.Select(c => $"{c.Rank} of {c.Suit}"));
                        Console.WriteLine($"{p.Name}: {result.Name} (Strength {result.Strength})");
                        Console.WriteLine($"   Hole Cards : {hole}");
                        Console.WriteLine($"   Kickers    : {result.KickersAsString}");
                    }
                    break;

                case GameEventType.Winner:
                    Console.WriteLine($"🏆 Winner: {player?.Name}");
                    break;
            }
        }

        private PlayerAction AskPlayerAction(IPlayer player, int currentBet, int minRaise)
        {
            Console.WriteLine($"\n{player.Name}, giliranmu!");
            Console.WriteLine($"Current Bet: {currentBet}, Balance: {player.Balance}");
            Console.WriteLine("Pilih aksi:");
            Console.WriteLine("1. Check");
            Console.WriteLine("2. Call");
            Console.WriteLine("3. Raise");
            Console.WriteLine("4. All-In");
            Console.WriteLine("5. Fold");

            while (true)
            {
                Console.Write("Pilihan: ");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "1": return PlayerAction.Check;
                    case "2": return PlayerAction.Call;
                    case "3": return PlayerAction.Raise;
                    case "4": return PlayerAction.AllIn;
                    case "5": return PlayerAction.Fold;
                    default:
                        Console.WriteLine("Input tidak valid. Pilih 1-5.");
                        break;
                }
            }
        }

        public void Run()
        {
            Console.WriteLine("Masukkan nickname untuk Player (Human): ");
            string humanName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(humanName)) humanName = "Player1";

            _game.AddPlayer(humanName, false);

            Console.Write("Masukkan jumlah bot (1-3): ");
            if (!int.TryParse(Console.ReadLine(), out int botCount)) botCount = 2;
            botCount = Math.Clamp(botCount, 1, 3);

            for (int i = 1; i <= botCount; i++)
                _game.AddPlayer($"Bot{i}", true);

            _game.StartGame();
        }
    }
}
