class Program
{
    static void Main(string[] args)
    {
        IDeck deck = new Deck();
        deck.Initialize();
        deck.Shuffle(new Random());

        ICard dummyCard = new Card(Suit.Spades, Rank.Ace);
        IChip dummyChip = new Chip(ChipType.White);
        IPlayer dummyPlayer = new HumanPlayer("Initializer");
        ITable table = new Table(deck);

        PokerGame game = new PokerGame(table, dummyCard, dummyChip, deck, dummyPlayer);

        // event handler
        game.OnGameEvent += (evt, p) =>
        {
            switch (evt)
            {
                case GameEventType.GameStarted:
                    Console.WriteLine("[EVENT] Game dimulai!");
                    break;
                case GameEventType.RoundStart:
                    Console.WriteLine("[EVENT] Round baru dimulai.");
                    break;
                case GameEventType.RoundEnded:
                    Console.WriteLine("[EVENT] Round berakhir.");
                    break;
                case GameEventType.PlayerFolded:
                    Console.WriteLine($"[EVENT] {p?.Name} melakukan Fold.");
                    break;
                case GameEventType.PlayerRaised:
                    Console.WriteLine($"[EVENT] {p?.Name} melakukan Raise.");
                    break;
                case GameEventType.PlayerAllin:
                    Console.WriteLine($"[EVENT] {p?.Name} melakukan All-In!");
                    break;
                case GameEventType.PlayerChecked:
                    Console.WriteLine($"[EVENT] {p?.Name} melakukan Check.");
                    break;
                case GameEventType.PlayerCalled:
                    Console.WriteLine($"[EVENT] {p?.Name} melakukan Call.");
                    break;
            }
        };

        // 👉 Panggil langsung menu utama dari StartGame
        game.StartGame();
    }
}

