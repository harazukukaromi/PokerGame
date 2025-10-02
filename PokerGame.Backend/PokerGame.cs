using System;
using System.Collections.Generic;
using System.Dynamic;
//Interface
public interface ICard
{
    Suit Suit { get; }
    Rank Rank { get; }
}
public interface IChip
{
    ChipType Type { get; }
}

public interface IDeck
{
    List<ICard> Cards { get; }
    int CardsRemaining { get; }
    void Initialize();
    void Shuffle(Random rng);
}
public interface IHand
{
    List<ICard> Cards { get; }
}
public interface IPlayer
{
    string Name { get; }
    IHand Hand { get; }
    List<Chip> Chips { get; }
    bool IsFolded { get; set; }
    int CurrentBet { get; set; }
    int Balance { get; set; }
    int TotalContributed { get; set; }
}

public interface ITable
{
    List<IPlayer> players { get; }
    IDeck Deck { get; }
    List<Chip> Pot { get; }
}

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
public class Hand : IHand
{
    private List<ICard> _cards = new();
    public List<ICard> Cards => _cards;

    public void AddCard(ICard card) => _cards.Add(card);
}

public class Chip : IChip
{
    public ChipType Type { get; }
    public Chip(ChipType type) => Type = type;
}

public class HumanPlayer : IPlayer
{
    public string Name { get; }
    public IHand Hand { get; } = new Hand();
    public List<Chip> Chips { get; } = new();
    public bool IsFolded { get; set; }
    public int CurrentBet { get; set; }
    public int Balance { get; set; }
    public int TotalContributed { get; set; }

    public HumanPlayer(string name, int initialChips = 1000)
    {
        Name = name;
        Balance = initialChips;
    }
}
public class AIPlayer : IPlayer
{
    public string Name { get; }
    public IHand Hand { get; } = new Hand();
    public List<Chip> Chips { get; } = new();
    public bool IsFolded { get; set; }
    public int CurrentBet { get; set; }
    public int Balance { get; set; }
    public int TotalContributed { get; set; }   

    public AIPlayer(string name, int initialChips = 1000)
    {
        Name = name;
        Balance = initialChips;
    }
}

public class Table : ITable
{
    public List<IPlayer> players { get; } = new();
    public IDeck Deck { get; }
    public List<Chip> Pot { get; } = new();

    public Table(IDeck deck)
    {
        Deck = deck ?? throw new ArgumentNullException(nameof(deck));
    }
}
public class PokerGame
{
    private readonly ITable _table;
    private readonly Random _random;
    private readonly List<ICard> _communityCards;
    private int _currentBet;
    private int _minRaise;
    private int _bigBlind;
    private int _smallBlindIndex;
    private int _bigBlindIndex;

    private readonly List<IPlayer> _players = new();

    public Action<GameEventType, IPlayer?>? OnGameEvent;
    public Action<IPlayer, string, int>? OnGameEnded;
    public Func<IPlayer, int, int, PlayerAction>? OnPlayerDecision;

    public PokerGame(ITable table, ICard card, IChip chip, IDeck deck, IPlayer player)//ICard card, IChip chip, IDeck deck, IPlayer player)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _random = new Random();
        _communityCards = new List<ICard>();

        // defaults
        _minRaise = 10;
        _bigBlind = 20;
        _smallBlindIndex = 0;
        _bigBlindIndex = 1;
    }
    //Class Management
    public void StartGame()
    {
        Console.WriteLine("=== Texas Hold'em Poker ===");
        Console.WriteLine("1. Play a Game");
        Console.WriteLine("2. Exit");

        string choice;
        while (true)
        {
            Console.Write("Pilih: ");
            choice = Console.ReadLine();
            if (choice == "1" || choice == "2") break;
            Console.WriteLine("Input tidak valid. Pilih 1 atau 2.");
        }

        if (choice == "2")
        {
            Console.WriteLine("Game berakhir. Terima kasih sudah bermain!");
            Environment.Exit(0);
        }

        // --- Input untuk Human ---
        Console.Write("Masukkan nickname untuk Player 1 (Human): ");
        string humanName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(humanName))
            humanName = "Player1";
        AddPlayer(humanName, false);

        // --- Input jumlah bot ---
        int botCount = 0;
        while (true)
        {
            Console.Write("Masukkan jumlah bot (1-3): ");
            string input = Console.ReadLine();
            if (int.TryParse(input, out botCount) && botCount >= 1 && botCount <= 3)
                break;
            Console.WriteLine("Input tidak valid. Harus antara 1 sampai 3.");
        }

        // --- Input nama tiap bot ---
        for (int i = 1; i <= botCount; i++)
        {
            Console.Write($"Masukkan nickname untuk Bot {i}: ");
            string botName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(botName))
                botName = $"Bot{i}";
            AddPlayer(botName, true);
        }

        _players.Clear();
        _players.AddRange(_table.players);

        OnGameEvent?.Invoke(GameEventType.GameStarted, null);

        if (_players.Count < 2)
        {
            Console.WriteLine("Tidak cukup pemain untuk memulai (minimal 2).");
            return;
        }

        PlayRound();
    }




    private void RotateBlinds()
    {
        if (_players.Count < 2) return;

        _smallBlindIndex = (_smallBlindIndex + 1) % _players.Count;
        _bigBlindIndex = (_smallBlindIndex + 1) % _players.Count;
    }
    public void PlayRound()
    {
        bool running = true;
        while (running)
        {
            //Console.WriteLine("\n=== Round Baru Dimulai ===");
            ResetDeck();
            ResetForNewRound();
            RotateBlinds();
            PostBlinds();
            DealCards();

            // === Pre-Flop ===
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            if (BettingRounds())
            {
                EndRound();
                continue;
            }

            // cek lagi setelah betting
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            // === Flop ===
            DealCommunityCards(3);
            ResetForNewStage();

            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            if (BettingRounds())
            {
                EndRound();
                continue;
            }

            // cek lagi setelah betting
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            // === Turn ===
            DealCommunityCards(1);
            ResetForNewStage();

            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            if (BettingRounds())
            {
                EndRound();
                continue;
            }

            // cek lagi setelah betting
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            // === River ===
            DealCommunityCards(1);
            ResetForNewStage();

            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            if (BettingRounds())
            {
                EndRound();
                continue;
            }

            // cek lagi terakhir
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                continue;
            }

            // === Showdown normal ===
            Showdown();
            DistributePot();
            EndRound();
        }
    }
    
    private void ResetRound()
    {
        _communityCards.Clear();
        _currentBet = 0;
        foreach (var p in _players)
        {
            p.IsFolded = false;
            p.CurrentBet = 0;
            p.Hand.Cards.Clear();
        }
        Console.WriteLine("Round baru dimulai.");
    }

    private void ResetForNewStage()
    {
        _currentBet = 0;
        foreach (var p in _players)
        {
            p.CurrentBet = 0;
        }
    }

    private void EndRound()
    {
        //Console.WriteLine("\n=== Ronde selesai ===");
        OnGameEvent?.Invoke(GameEventType.RoundEnded, null); // <-- trigger event
        // reset pot
        _table.Pot.Clear();

        // eliminasi pemain bangkrut setelah showdown & distribusi pot
        var eliminated = _players
        .Where(p => p.Balance <= 0 || p.Balance < (int)ChipType.White) // 💡 White = 10
        .ToList();
        foreach (var p in eliminated)
            RemovePlayer(p);

        // tampilkan state meja
        ShowTableState();

        // ✅ tanya ke player apakah lanjut
        string choice;
        while (true)
        {
            Console.Write("Lanjut main? (1 = Ya, 2 = Keluar): ");
            choice = Console.ReadLine();
            if (choice == "1" || choice == "2") break;
            Console.WriteLine("Input tidak valid. Pilih 1 atau 2.");
        }

        if (choice == "2")
        {
            Console.WriteLine("Game berakhir. Terima kasih sudah bermain!");
            Environment.Exit(0);
        }
    }
    private void DistributePot()
    {
        // Ambil semua pemain yang masih aktif
        var activePlayers = _players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count == 0) return;

        // Cari strength tertinggi dari showdown
        var results = activePlayers
            .Select(p => (player: p, result: EvaluateHand(p.Hand.Cards.ToList(), _communityCards)))
            .ToList();

        int bestStrength = results.Max(r => r.result.Strength);
        var bestCandidates = results.Where(r => r.result.Strength == bestStrength).ToList();

        if (bestCandidates.Count == 0) return;

        // Bandingkan kickers
        var winners = new List<IPlayer>();
        var bestResult = bestCandidates.First().result;
        winners.Add(bestCandidates.First().player);

        for (int i = 1; i < bestCandidates.Count; i++)
        {
            int cmp = CompareKickers(bestCandidates[i].result.Kickers, bestResult.Kickers);
            if (cmp > 0)
            {
                winners.Clear();
                winners.Add(bestCandidates[i].player);
                bestResult = bestCandidates[i].result;
            }
            else if (cmp == 0)
            {
                winners.Add(bestCandidates[i].player);
            }
        }

        // Distribusi chip berdasarkan kontribusi side pot
        SplitPotEvenlyAmong(winners);

        // Reset kontribusi semua pemain untuk ronde berikutnya
        foreach (var p in _players)
            p.TotalContributed = 0;
    }

    private void UpdatePlayerHandStates(string stage)
    {
        Console.WriteLine($"\n== Evaluasi Hand ({stage}) ==");

        foreach (var p in _players)
        {
            if (p.IsFolded) continue;

            var playerCards = p.Hand.Cards.ToList();
            var result = EvaluateHand(playerCards, _communityCards);

            if (p is HumanPlayer || stage == "Showdown")
            {
                string hole = string.Join(", ", playerCards.Select(c => $"{c.Rank} of {c.Suit}"));
                string kickerNames = !string.IsNullOrEmpty(result.KickersAsString) ? result.KickersAsString : "-";

                Console.WriteLine($"{p.Name}: {result.Name} (Strength {result.Strength})");
                Console.WriteLine($"   Hole Cards : {hole}");
                Console.WriteLine($"   Kickers    : {kickerNames}");
            }
            else
            {
                Console.WriteLine($"{p.Name}: [Cards Hidden]");
            }
        }
    }


    private bool AllPlayersAllInOrFolded()
    {
        var activePlayers = _players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count <= 1) return true; // tinggal 1 pemain
        return activePlayers.All(p => p.Balance == 0); // semua All-In
    }

    private void RevealRemainingCardsAndShowdown()
    {
        int remaining = 5 - _communityCards.Count;
        if (remaining > 0)
        {
            Console.WriteLine($"\n-- Dealing remaining {remaining} community card(s) --");
            DealCommunityCards(remaining);
        }

        Console.WriteLine("Semua pemain sudah All-In → langsung Showdown!");
        Showdown();
        DistributePot();
    }

    

    private void DealCards()
    {
        Console.WriteLine("\n-- Dealing hole cards --");
        foreach (var p in _players)
        {
            var c1 = DealCardDeck();
            var c2 = DealCardDeck();
            ((Hand)p.Hand).AddCard(c1);
            ((Hand)p.Hand).AddCard(c2);

            if (p is HumanPlayer)
            {
                string holeCards = $"{c1.Rank} of {c1.Suit}, {c2.Rank} of {c2.Suit}";
                Console.WriteLine($"{p.Name} gets: {holeCards}");
            }
            else
            {
                Console.WriteLine($"{p.Name} gets: [Hidden]");
            }
        }
    }


    private void DealCommunityCards(int count)
    {
        Console.WriteLine($"\n-- Dealing {count} community card(s) --");
        for (int i = 0; i < count; i++)
            _communityCards.Add(DealCardDeck());

        ShowBoard();

        string stage = _communityCards.Count switch
        {
            3 => "Flop",
            4 => "Turn",
            5 => "River",
            _ => "Board"
        };

        UpdatePlayerHandStates(stage);
    }


    private void ShowBoard()//tambahan kelas untuk menampilkan class untuk show card
    {
        Console.WriteLine("Board:");
        if (_communityCards.Count == 0) Console.WriteLine("  (empty)");
        else
        {
            foreach (var c in _communityCards)
                Console.WriteLine($"  {c.Rank} of {c.Suit}");
        }
    }
    private void PostBlinds()
    {
        if (_players.Count < 2) return;

        var sb = _players[_smallBlindIndex % _players.Count];
        var bb = _players[_bigBlindIndex % _players.Count];

        int smallAmt = _bigBlind / 2;
        smallAmt = (int)(Math.Round(smallAmt / 10.0) * 10);

        // Small Blind
        AddToPot(smallAmt);            
        sb.Balance -= smallAmt;        
        sb.CurrentBet = smallAmt;
        sb.TotalContributed += smallAmt;   
        Console.WriteLine($"{sb.Name} posts small blind {FormatChips(smallAmt)} (Chips: {FormatChips(sb.Balance)})");

        // Big Blind (special case jika balance kurang dari big blind)
        int bbAmt = Math.Min(_bigBlind, bb.Balance);

        AddToPot(bbAmt);
        bb.Balance -= bbAmt;
        bb.CurrentBet = bbAmt;
        bb.TotalContributed += bbAmt;

        if (bbAmt < _bigBlind)
        {
            Console.WriteLine($"{bb.Name} posts big blind {FormatChips(bbAmt)} (ALL-IN) (Chips: {FormatChips(bb.Balance)})");
        }
        else
        {
            Console.WriteLine($"{bb.Name} posts big blind {FormatChips(bbAmt)} (Chips: {FormatChips(bb.Balance)})");
        }

        // tetap set currentBet = _bigBlind supaya call minimum tidak turun
        _currentBet = _bigBlind;
    }


    private bool BettingRounds()
    {
        Console.WriteLine("\n-- Betting Round --");

        bool isPreFlop = _communityCards.Count == 0;

        var activePlayers = _players.Where(p => !p.IsFolded).ToList();
        int activeWithChips = activePlayers.Count(p => p.Balance > 0);
        int allInPlayers = activePlayers.Count(p => p.Balance == 0);

        // ✅ Kalau semua sudah all-in → biarkan PlayRound yang reveal board penuh
        if (activePlayers.All(p => p.Balance == 0))
        {
            Console.WriteLine("Semua pemain sudah All-In → lanjut board penuh & showdown.");
            return false; // jangan akhiri ronde di sini
        }

        // ✅ Kalau masih ada 1 pemain aktif dengan chip, sisanya all-in
        if (activeWithChips <= 1 && allInPlayers > 0)
        {
            Console.WriteLine("Semua lawan sudah All-In → lanjut board penuh & showdown.");
            return false; // biar PlayRound reveal otomatis
        }

        // ✅ Kalau tinggal 1 pemain (lainnya fold semua)
        if (activeWithChips <= 1 && allInPlayers == 0)
        {
            return true; // akhiri ronde → pemenang otomatis
        }

        // --- proses betting normal ---
        int startIndex = isPreFlop
            ? (_bigBlindIndex + 1) % _players.Count
            : (_smallBlindIndex + 1) % _players.Count;

        // cari pemain pertama yang bisa bertindak
        int attempts = 0;
        while ((_players[startIndex].IsFolded || _players[startIndex].Balance == 0) && attempts < _players.Count)
        {
            startIndex = (startIndex + 1) % _players.Count;
            attempts++;
        }

        if (attempts >= _players.Count)
        {
            Console.WriteLine("Tidak ada pemain yang bisa bertindak → lanjut showdown.");
            return false;
        }

        int idx = startIndex;
        int lastAggressorIndex = -1;
        bool hasActed = false;
        bool firstLoopCompleted = false;

        while (true)
        {
            activePlayers = _players.Where(p => !p.IsFolded).ToList();

            // cek lagi: semua aktif all-in
            if (activePlayers.All(p => p.Balance == 0))
            {
                Console.WriteLine("Semua pemain sudah All-In → lanjut board penuh & showdown.");
                return false; // biar PlayRound reveal
            }

            // kalau tinggal 1 pemain
            if (activePlayers.Count == 1)
            {
                var winner = activePlayers.First();
                Console.WriteLine($"{winner.Name} menang otomatis (semua lawan fold).");

                int potValue = GetPotValue();
                AddChipsToPlayerByAmount(winner, potValue);
                Console.WriteLine($"{winner.Name} receives {FormatChips(potValue)} from pot.");
                _table.Pot.Clear();
                return true; // ronde selesai
            }

            bool allMatched = activePlayers.All(p => p.CurrentBet == _currentBet || p.Balance == 0);
            bool roundClosed = (lastAggressorIndex != -1 && idx == lastAggressorIndex);

            if (firstLoopCompleted && hasActed && allMatched && (lastAggressorIndex == -1 || roundClosed))
                return false; // betting round selesai normal

            var player = _players[idx];

            if (!player.IsFolded && player.Balance > 0)
            {
                int prevBet = player.CurrentBet;
                PlayerAction action = MakeDecision(player, _currentBet, _minRaise);
                ProcessPlayerTurn(player, action);
                hasActed = true;

                if ((action == PlayerAction.Raise && player.CurrentBet > prevBet) ||
                    (action == PlayerAction.AllIn && player.CurrentBet > prevBet))
                {
                    lastAggressorIndex = idx;
                }
            }

            idx = (idx + 1) % _players.Count;
            if (idx == startIndex)
                firstLoopCompleted = true;
        }
    }

    private void ProcessPlayerTurn(IPlayer player, PlayerAction action)
    {
        int toCall = _currentBet - player.CurrentBet;

        switch (action)
        {
            case PlayerAction.Fold:
            if (player == null) return;
            player.IsFolded = true;
            OnGameEvent?.Invoke(GameEventType.PlayerFolded, player);
            break;

            case PlayerAction.Check:
            // Console.WriteLine($"{player.Name} checks.");   //
            OnGameEvent?.Invoke(GameEventType.PlayerChecked, player);
            break;

            case PlayerAction.Call:
            if (toCall <= 0)
            {
                // Console.WriteLine($"{player.Name} checks (no bet to call).");  //
                OnGameEvent?.Invoke(GameEventType.PlayerChecked, player);
            }
            else
            {
                int callAmount = Math.Min(toCall, player.Balance);
                callAmount = (int)(Math.Round(callAmount / 10.0) * 10);

                AddToPot(callAmount);
                player.Balance -= callAmount;
                player.CurrentBet += callAmount;
                player.TotalContributed += callAmount;   

                // Console.WriteLine($"{player.Name} calls {FormatChips(callAmount)} (Balance: {player.Balance})"); // ❌ hapus ini
                // Console.WriteLine($"Pot sekarang = {FormatChips(GetPotValue())}"); // boleh dihapus juga kalau mau full event
                OnGameEvent?.Invoke(GameEventType.PlayerCalled, player);
            }
            break;

            case PlayerAction.Raise:
            int minRaise = 10;
            int maxRaise = player.Balance;

            if (maxRaise <= toCall + minRaise)
            {
                Console.WriteLine($"{player.Name} tidak cukup balance untuk Raise, otomatis All-In.");
                goto case PlayerAction.AllIn;
            }

            int raiseAmount;
            if (player is HumanPlayer)
            {
                int minRaiseAmount = toCall + minRaise;
                int maxRaiseAmount = player.Balance;

                while (true)
                {
                    Console.WriteLine($"{player.Name}, masukkan jumlah Raise (minimal {minRaiseAmount}, maksimal {maxRaiseAmount}, kelipatan 10):");
                    string input = Console.ReadLine();

                    if (!int.TryParse(input, out raiseAmount))
                    {
                        Console.WriteLine("Input tidak valid. Harus berupa angka.");
                        continue;
                    }

                    if (raiseAmount < minRaiseAmount || raiseAmount > maxRaiseAmount)
                    {
                        Console.WriteLine($"Input tidak valid. Harus di antara {minRaiseAmount} dan {maxRaiseAmount}.");
                        continue;
                    }

                    int rounded = (int)(Math.Round(raiseAmount / 10.0) * 10);
                    if (rounded != raiseAmount)
                    {
                        Console.WriteLine($"Input {raiseAmount} dibulatkan menjadi {rounded}.");
                        raiseAmount = rounded;
                    }
                    break;
                    }
                }
            else
            {
                raiseAmount = _random.Next(toCall + minRaise, maxRaise + 1);
                raiseAmount = (int)(Math.Round(raiseAmount / 10.0) * 10);
            }

            AddToPot(raiseAmount);
            player.Balance -= raiseAmount;
            if (player.Balance < 0) player.Balance = 0;

            player.CurrentBet += raiseAmount;
            player.TotalContributed += raiseAmount;   
            _currentBet = player.CurrentBet;

            string chipText = FormatChips(raiseAmount); //
            Console.WriteLine($"{player.Name} raises {chipText} (Chips left: {FormatChips(player.Balance)})");
            Console.WriteLine($"Pot sekarang = {FormatChips(GetPotValue())}");
            OnGameEvent?.Invoke(GameEventType.PlayerRaised, player);
            break;

            case PlayerAction.AllIn:
            int allInAmount = player.Balance;
            allInAmount = (int)(Math.Round(allInAmount / 10.0) * 10);

            AddToPot(allInAmount);
            player.Balance -= allInAmount;
            if (player.Balance < 0) player.Balance = 0;

            player.CurrentBet += allInAmount;
            player.TotalContributed += allInAmount;   
            if (player.CurrentBet > _currentBet)
                _currentBet = player.CurrentBet;

            //Console.WriteLine($"{player.Name} goes ALL-IN with {FormatChips(allInAmount)} (Chips: {FormatChips(player.Balance)})");
            Console.WriteLine($"Pot sekarang = {FormatChips(GetPotValue())}");
            OnGameEvent?.Invoke(GameEventType.PlayerAllin, player);
            break;
        }
    }

    private void Showdown()
    {
        Console.WriteLine("\n-- Showdown --");
        List<(IPlayer player, HandResult result)> results = new();

        // kumpulkan hasil evaluasi hand
        foreach (var p in _players)
        {
            if (p.IsFolded) continue;

            var playerCards = p.Hand.Cards.ToList();
            var result = EvaluateHand(playerCards, _communityCards);
            results.Add((p, result));

            string holeCards = string.Join(", ", playerCards.Select(c => $"{c.Rank} of {c.Suit}"));

            // tampilkan kicker (aman walau kosong)
            string kickerNames = !string.IsNullOrEmpty(result.KickersAsString) ? result.KickersAsString : "-";

            Console.WriteLine($"{p.Name}: {result.Name} (Strength: {result.Strength})");
            Console.WriteLine($"   Hole Cards : {holeCards}");
            Console.WriteLine($"   Kickers    : {kickerNames}");
        }

        // jika semua fold kecuali satu
        var activePlayers = _players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count == 1)
        {
            var soleWinner = activePlayers.First();
            Console.WriteLine($"Winner: {soleWinner.Name} (all others folded)");
            var potChips = _table.Pot.ToList();
            _table.Pot.Clear();
            int potValue = potChips.Sum(c => (int)c.Type);
            AddChipsListToPlayer(soleWinner, potChips);
            Console.WriteLine($"{soleWinner.Name} receives {FormatChips(potValue)} from pot.");
        }

        // kalau tidak ada pemain sama sekali
        if (results.Count == 0)
        {
            Console.WriteLine("No players in showdown.");
            return;
        }

        // cari strength terbaik
        int bestStrength = results.Max(r => r.result.Strength);
        var bestCandidates = results.Where(r => r.result.Strength == bestStrength).ToList();

        if (bestCandidates == null || bestCandidates.Count == 0)
        {
            Console.WriteLine("No candidates for winner.");
            return;
        }

        var winners = new List<IPlayer>();
        HandResult bestResult = bestCandidates.First().result;
        winners.Add(bestCandidates.First().player);

        for (int i = 1; i < bestCandidates.Count; i++)
        {
            int cmp = CompareKickers(bestCandidates[i].result.Kickers, bestResult.Kickers);
            if (cmp > 0)
            {
                winners.Clear();
                winners.Add(bestCandidates[i].player);
                bestResult = bestCandidates[i].result;
            }
            else if (cmp == 0)
            {
                winners.Add(bestCandidates[i].player);
            }
        }

        // tampilkan pemenang (tidak langsung membayar di sini)
        if (winners.Count == 1)
        {
            var winPlayer = winners.First();
            Console.WriteLine($"Winner: {winPlayer.Name} with {results.First(r => r.player == winPlayer).result.Name}");
        }
        else
        {
            Console.WriteLine("Tie between: " + string.Join(", ", winners.Select(w => w.Name)));
        }
    }



   public class HandResult
    {
        public string Name { get; set; } = "High Card";
        public int Strength { get; set; } = 1;
        public List<ICard> Kickers { get; set; } = new();

        public string KickersAsString => string.Join(", ",
            Kickers.Select(c => $"{c.Rank} of {c.Suit}"));
    }

    private HandResult EvaluateBestHand(List<ICard> cards)
     {
        var allCombos = GetCombinations(cards, 5);
        HandResult best = new HandResult();
        foreach (var combo in allCombos)
        {
            var result = EvaluateFiveCardHand(combo);
            if (result.Strength > best.Strength ||
                (result.Strength == best.Strength && CompareKickers(result.Kickers, best.Kickers) > 0))
                best = result;
        }
        return best;
    }

    private HandResult EvaluateFiveCardHand(List<ICard> cards)
    {
        var sorted = cards.OrderByDescending(c => c.Rank).ToList();
        var groups = cards.GroupBy(c => c.Rank)
                        .OrderByDescending(g => g.Count())
                        .ThenByDescending(g => g.Key)
                        .ToList();

        // Royal Flush
        if (IsRoyalFlush(cards))
            return new HandResult { Name = "Royal Flush", Strength = GetHandStrength("Royal Flush"), Kickers = sorted };

        // Straight Flush
        if (IsStraightFlush(cards))
            return new HandResult { Name = "Straight Flush", Strength = GetHandStrength("Straight Flush"), Kickers = sorted };

        // Four of a Kind
        if (IsFourOfAKind(cards))
        {
            var quad = groups.First(g => g.Count() == 4).ToList();
            var kicker = cards.Except(quad).OrderByDescending(c => c.Rank).First();
            return new HandResult { Name = "Four of a Kind", Strength = GetHandStrength("Four of a Kind"), Kickers = quad.Concat(new[] { kicker }).ToList() };
        }

        // Full House
        if (IsFullHouse(cards))
        {
            var three = groups.First(g => g.Count() == 3).ToList();
            var pair = groups.First(g => g.Count() == 2).ToList();
            return new HandResult { Name = "Full House", Strength = GetHandStrength("Full House"), Kickers = three.Concat(pair).ToList() };
        }

        // Flush
        if (IsFlush(cards))
        {
            var flushSuit = cards.GroupBy(c => c.Suit).First(g => g.Count() >= 5).Key;
            var flushCards = cards.Where(c => c.Suit == flushSuit).OrderByDescending(c => c.Rank).Take(5).ToList();
            return new HandResult { Name = "Flush", Strength = GetHandStrength("Flush"), Kickers = flushCards };
        }

        // Straight
        if (IsStraight(cards))
        {
            var distinctRanks = cards.GroupBy(c => c.Rank).Select(g => g.First()).OrderByDescending(c => c.Rank).ToList();
            var straightCards = new List<ICard>();
            for (int i = 0; i < distinctRanks.Count - 4; i++)
            {
                var window = distinctRanks.Skip(i).Take(5).ToList();
                if (window.Max(c => (int)c.Rank) - window.Min(c => (int)c.Rank) == 4)
                {
                    straightCards = window.OrderByDescending(c => c.Rank).ToList();
                    break;
                }
            }
            if (straightCards.Count == 0)
                straightCards = distinctRanks.Take(5).ToList();

            return new HandResult { Name = "Straight", Strength = GetHandStrength("Straight"), Kickers = straightCards };
        }

        // Three of a Kind
        if (IsThreeOfAKind(cards))
        {
            var trips = groups.First(g => g.Count() == 3).ToList();
            var kickers = cards.Except(trips).OrderByDescending(c => c.Rank).Take(2).ToList();
            return new HandResult { Name = "Three of a Kind", Strength = GetHandStrength("Three of a Kind"), Kickers = trips.Concat(kickers).ToList() };
        }

        // Two Pair
        if (IsTwoPair(cards))
        {
            var pairs = groups.Where(g => g.Count() == 2).OrderByDescending(g => g.Key).Take(2).SelectMany(g => g).ToList();
            var kicker = cards.Except(pairs).OrderByDescending(c => c.Rank).First();
            return new HandResult { Name = "Two Pair", Strength = GetHandStrength("Two Pair"), Kickers = pairs.Concat(new[] { kicker }).ToList() };
        }

        // One Pair
        if (IsOnePair(cards))
        {
            var pair = groups.First(g => g.Count() == 2).ToList();
            var kickers = cards.Except(pair).OrderByDescending(c => c.Rank).Take(3).ToList();
            return new HandResult { Name = "One Pair", Strength = GetHandStrength("One Pair"), Kickers = pair.Concat(kickers).ToList() };
        }

        // High Card
        return new HandResult { Name = $"High Card {sorted.First().Rank}", Strength = GetHandStrength("High Card"), Kickers = sorted.Take(5).ToList() };
    }

    public IEnumerable<List<ICard>> GetCombinations(List<ICard> cards, int k)
    {
        int n = cards.Count;
        if (k > n) yield break;
        int[] indices = new int[k];
        for (int i = 0; i < k; i++) indices[i] = i;

        while (true)
        {
            yield return indices.Select(i => cards[i]).ToList();
            int t = k - 1;
            while (t >= 0 && indices[t] == n - k + t) t--;
            if (t < 0) yield break;
            indices[t]++;
            for (int i = t + 1; i < k; i++) indices[i] = indices[i - 1] + 1;
        }
    }
    public HandResult EvaluateHand(List<ICard> handCards, List<ICard> communityCards)
    {
        var allCards = handCards.Concat(communityCards).ToList();

        // PRE-FLOP
        if (communityCards.Count == 0 && handCards.Count == 2)
        {
            var c1 = handCards[0];
            var c2 = handCards[1];

            if (c1.Rank == c2.Rank)
            {
                return new HandResult
                {
                    Name = "One Pair",
                    Strength = GetHandStrength("One Pair"),
                    Kickers = new List<ICard> { c1, c2 }
                };
            }
            else
            {
                var sorted = handCards.OrderByDescending(c => c.Rank).ToList();
                return new HandResult
                {
                    Name = $"High Card {sorted.First().Rank}",
                    Strength = GetHandStrength("High Card"),
                    Kickers = sorted
                };
            }
        }

        // FLOP / TURN / RIVER
        return EvaluateBestHand(allCards);
    }


    public int GetHandStrength(string hand) => hand switch
    {
        "Royal Flush" => 10,
        "Straight Flush" => 9,
        "Four of a Kind" => 8,
        "Full House" => 7,
        "Flush" => 6,
        "Straight" => 5,
        "Three of a Kind" => 4,
        "Two Pair" => 3,
        "One Pair" => 2,
        "High Card" => 1,
        _ => 0
    };
    private int CompareKickers(List<ICard> k1, List<ICard> k2)
    {
        if (k1 == null && k2 == null) return 0;
        if (k1 == null) return -1;
        if (k2 == null) return 1;

        int count = Math.Max(k1.Count, k2.Count);
        for (int i = 0; i < count; i++)
        {
            int v1 = i < k1.Count ? (int)k1[i].Rank : 0;
            int v2 = i < k2.Count ? (int)k2[i].Rank : 0;

            if (v1 > v2) return 1;
            if (v1 < v2) return -1;
        }
        return 0;
    }
    private bool IsRoyalFlush(List<ICard> cards) =>
        IsStraightFlush(cards) && cards.Any(c => c.Rank == Rank.Ace) && cards.Any(c => c.Rank == Rank.King);
    private bool IsStraightFlush(List<ICard> cards) =>
        IsFlush(cards) && IsStraight(cards);
    private bool IsFourOfAKind(List<ICard> cards) =>
        cards.GroupBy(c => c.Rank).Any(g => g.Count() == 4);
    private bool IsFullHouse(List<ICard> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).ToList();
        return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
    }
    private bool IsFlush(List<ICard> cards) =>
        cards.GroupBy(c => c.Suit).Any(g => g.Count() == 5);

    private bool IsStraight(List<ICard> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();

        // Ace sebagai 1 (low straight A-2-3-4-5)
        if (ranks.Contains((int)Rank.Ace))
            ranks.Insert(0, 1);

        int consecutive = 1;
        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] == ranks[i - 1] + 1)
            {
                consecutive++;
                if (consecutive >= 5) return true;
            }
            else
            {
                consecutive = 1;
            }
        }
        return false;
    }

    private bool IsThreeOfAKind(List<ICard> cards) =>
        cards.GroupBy(c => c.Rank).Any(g => g.Count() == 3);
    private bool IsTwoPair(List<ICard> cards) =>
        cards.GroupBy(c => c.Rank).Count(g => g.Count() == 2) >= 2;
    private bool IsOnePair(List<ICard> cards) =>
        cards.GroupBy(c => c.Rank).Any(g => g.Count() == 2);
    public void AddPlayer(string name, bool isAI)
    {
        if (_table.players.Count >= 4)
        {
            Console.WriteLine("Maksimal 4 pemain diperbolehkan di meja.");
            return;
        }

        if (_table.players.Any(p => p.Name == name || p.Name == name + " (Bot)"))
        {
            Console.WriteLine($"Player {name} sudah ada di meja.");
            return;
        }

        string finalName = isAI ? name + " (Bot)" : name;
        IPlayer player = isAI ? new AIPlayer(finalName, 1000) : new HumanPlayer(finalName, 1000);

        _table.players.Add(player);
        Console.WriteLine($"{player.Name} bergabung ke meja. Total players: {_table.players.Count}/4");
    }

    public void RemovePlayer(IPlayer player)    
    {
        if (_table.players.Remove(player))
        {
            Console.WriteLine($"{player.Name} keluar dari meja. Total players: {_table.players.Count}/4");

            // Sesuaikan index blind setelah remove
            if (_table.players.Count > 0)
            {
                _smallBlindIndex %= _table.players.Count;
                _bigBlindIndex %= _table.players.Count;
            }
        }

        // Cek kalau semua human sudah habis
        if (!_table.players.Any(p => p is HumanPlayer && p.Balance > 0))
        {
            Console.WriteLine("Anda sudah kehabisan chip. Game berakhir!");
            Environment.Exit(0);
        }

        // Tambahan: Cek kalau hanya human tersisa
        if (_table.players.Count == 1 && _table.players.First() is HumanPlayer)
        {
            Console.WriteLine("You win! Semua bot sudah kalah. Game berakhir!");
            Environment.Exit(0);
        }
    }

    public List<IPlayer> GetPlayers() => _table.players;
    public void AddCard(ICard card) => _communityCards.Add(card);
    public void ClearCard() => _communityCards.Clear();
    private void ResetForNewRound()
    {
        _communityCards.Clear();
        _currentBet = 0;

        foreach (var p in _players)
        {
            p.IsFolded = false;
            p.CurrentBet = 0;
            p.Hand.Cards.Clear();
            p.TotalContributed = 0; 
        }

        _players.Clear();
        _players.AddRange(_table.players);

        //Console.WriteLine("Round baru dimulai.");
        OnGameEvent?.Invoke(GameEventType.RoundStart, null); // <-- trigger event
    }
    public bool PlaceBet(int amount)
    {
        if (amount < _currentBet) return false;
        _currentBet = amount;
        return true;
    }

    public ICard DealCardDeck()
    {
        var deck = _table.Deck.Cards;
        if (deck.Count == 0) throw new InvalidOperationException("Deck kosong!");
        var card = deck[0];
        deck.RemoveAt(0);
        return card;
    }
    public void ResetDeck()
    {
        if (_table.Deck == null) throw new InvalidOperationException("Table deck is null.");
        _table.Deck.Initialize();
        _table.Deck.Shuffle(_random);
    }
    
    private void ShuffleDeck()// class tambahan sementar untuk mengocok deck
    {
        var deck = _table.Deck.Cards;
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (deck[k], deck[n]) = (deck[n], deck[k]);
        }
    }
    public IDeck GotDeck() => _table.Deck;
    public int GetPot() => 20;
    public void AddToPot(int amount)
    {
        if (amount <= 0) return;

        var chips = new List<Chip>();
        int remaining = amount;

        while (remaining >= (int)ChipType.Black) { chips.Add(new Chip(ChipType.Black)); remaining -= (int)ChipType.Black; }
        while (remaining >= (int)ChipType.Green) { chips.Add(new Chip(ChipType.Green)); remaining -= (int)ChipType.Green; }
        while (remaining >= (int)ChipType.Red)   { chips.Add(new Chip(ChipType.Red));   remaining -= (int)ChipType.Red; }
        while (remaining >= (int)ChipType.White) { chips.Add(new Chip(ChipType.White)); remaining -= (int)ChipType.White; }

        AddChipsListToPot(chips);
    }


    public void RecieveCard(ICard card) => _communityCards.Add(card);

    public PlayerAction MakeDecision(IPlayer player, int currentBet, int minRaise)
    {
        int playerChips = player.Chips.Sum(c => (int)c.Type);
        int toCall = currentBet - player.CurrentBet;

        if (player is AIPlayer)
        {
            var choices = new List<PlayerAction>();
            if (toCall <= 0)
            {
                choices.Add(PlayerAction.Check);
                if (playerChips >= minRaise) choices.Add(PlayerAction.Raise);
                if (playerChips > 0) choices.Add(PlayerAction.AllIn);
            }
            else
            {
                choices.Add(PlayerAction.Call);
                choices.Add(PlayerAction.Fold);
                if (playerChips >= toCall + minRaise) choices.Add(PlayerAction.Raise);
                if (playerChips > 0) choices.Add(PlayerAction.AllIn);
            }
            return choices[_random.Next(choices.Count)];
        }

        // Human
        if (toCall <= 0)
        {
            string input;
            while (true)
            {
                Console.WriteLine($"{player.Name}, pilih aksi: [1] Check, [2] Raise, [3] AllIn, [4] Fold");
                input = Console.ReadLine();
                if (input == "1" || input == "2" || input == "3" || input == "4")
                    break;
                Console.WriteLine("Input tidak valid. Harus pilih 1, 2, 3, atau 4.");
            }

            return input switch
            {
                "1" => PlayerAction.Check,
                "2" => PlayerAction.Raise,
                "3" => PlayerAction.AllIn,
                "4" => PlayerAction.Fold,
                _ => PlayerAction.Check
            };
        }
        else
        {
            string input;
            while (true)
            {
                Console.WriteLine($"{player.Name}, ada bet {currentBet}. Pilih aksi: [1] Call, [2] Raise, [3] AllIn, [4] Fold");
                input = Console.ReadLine();
                if (input == "1" || input == "2" || input == "3" || input == "4")
                    break;
                Console.WriteLine("Input tidak valid. Harus pilih 1, 2, 3, atau 4.");
            }

            return input switch
            {
                "1" => PlayerAction.Call,
                "2" => PlayerAction.Raise,
                "3" => PlayerAction.AllIn,
                "4" => PlayerAction.Fold,
                _ => PlayerAction.Call
            };
        }
    }
    private string FormatChips(int amount)
    {
        if (amount <= 0) return "0";

        var chips = new List<Chip>();
        int remaining = amount;

        while (remaining >= (int)ChipType.Black) { chips.Add(new Chip(ChipType.Black)); remaining -= (int)ChipType.Black; }
        while (remaining >= (int)ChipType.Green) { chips.Add(new Chip(ChipType.Green)); remaining -= (int)ChipType.Green; }
        while (remaining >= (int)ChipType.Red)   { chips.Add(new Chip(ChipType.Red));   remaining -= (int)ChipType.Red; }
        while (remaining >= (int)ChipType.White) { chips.Add(new Chip(ChipType.White)); remaining -= (int)ChipType.White; }

        NormalizeChips(chips);

        var grouped = chips
            .GroupBy(c => c.Type)
            .OrderByDescending(g => g.Key) // urut besar → kecil
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        return string.Join(" + ", grouped) + $" ({amount})";
    }


    private List<Chip> RemoveChipsFromPlayer(IPlayer player, int amount)
    {
        var removed = new List<Chip>();

        // ambil maksimal sesuai balance pemain
        int toTake = Math.Min(amount, player.Balance);
        if (toTake <= 0) return removed;

        // kurangi balance langsung
        player.Balance -= toTake;
        if (player.Balance < 0) player.Balance = 0;

        // buat chips yang mewakili jumlah yang diambil (greedy: terbesar dulu)
        int remaining = toTake;
        while (remaining >= (int)ChipType.Black) { removed.Add(new Chip(ChipType.Black)); remaining -= (int)ChipType.Black; }
        while (remaining >= (int)ChipType.Green) { removed.Add(new Chip(ChipType.Green)); remaining -= (int)ChipType.Green; }
        while (remaining >= (int)ChipType.Red) { removed.Add(new Chip(ChipType.Red)); remaining -= (int)ChipType.Red; }
        while (remaining >= (int)ChipType.White) { removed.Add(new Chip(ChipType.White)); remaining -= (int)ChipType.White; }

        // Jika ada sisa negatif (seharusnya tidak), kita abaikan — but toTake sudah dikalkulasi.
        return removed;
    }

    private void AddChipsListToPot(IEnumerable<Chip> chips)
    {
        if (chips == null) return;
        _table.Pot.AddRange(chips);
        NormalizeChips(_table.Pot); // keep pot normalized
    }

    private void AddChipsListToPlayer(IPlayer player, IEnumerable<Chip> chips)
    {
        if (chips == null) return;
        var chipList = chips.ToList();
        player.Chips.AddRange(chipList);
        NormalizeChips(player.Chips);

        //update balance juga
        int amount = chipList.Sum(c => (int)c.Type);
        player.Balance += amount;
    }


    private void NormalizeChips(List<Chip> chips)
    {
        if (chips == null) return;
        // White -> Red : 5 whites -> 1 red
        while (chips.Count(c => c.Type == ChipType.White) >= 5)
        {
            for (int i = 0; i < 5; i++) chips.Remove(chips.First(c => c.Type == ChipType.White));
            chips.Add(new Chip(ChipType.Red));
        }
        // Red -> Green : 2 reds -> 1 green
        while (chips.Count(c => c.Type == ChipType.Red) >= 2)
        {
            for (int i = 0; i < 2; i++) chips.Remove(chips.First(c => c.Type == ChipType.Red));
            chips.Add(new Chip(ChipType.Green));
        }
        // Green -> Black : 10 greens -> 1 black
        while (chips.Count(c => c.Type == ChipType.Green) >= 10)
        {
            for (int i = 0; i < 10; i++) chips.Remove(chips.First(c => c.Type == ChipType.Green));
            chips.Add(new Chip(ChipType.Black));
        }
    }

    private int GetPotValue() =>
        _table.Pot.Sum(c => (int)c.Type);

    private void SplitPotEvenlyAmong(List<IPlayer> winners)
    {
        if (winners == null || winners.Count == 0) return;

        int totalPot = GetPotValue();

        // kalau tidak ada all-in → bagi langsung total pot
        bool noAllIn = _players.All(p => p.Balance > 0 || p.IsFolded);
        if (noAllIn)
        {
            var active = _players.Where(p => !p.IsFolded).ToList();
            var potWinners = winners.Intersect(active).ToList();
            if (potWinners.Count == 0) return;

            int share = totalPot / potWinners.Count;
            int rem = totalPot % potWinners.Count;

            for (int i = 0; i < potWinners.Count; i++)
            {
                int give = share + (i == 0 ? rem : 0); 
                AddChipsToPlayerByAmount(potWinners[i], give);
                Console.WriteLine($"{potWinners[i].Name} receives {FormatChips(give)} from pot.");
            }

            _table.Pot.Clear();
            return; // selesai
        }

        // --- kalau ada all-in, pakai side pot logic per-level ---
        var contributions = new Dictionary<IPlayer, int>();
        foreach (var p in _players)
            contributions[p] = p.TotalContributed;

        // players yang masih showdown (tidak fold) dan punya kontribusi > 0
        var showdownPlayers = _players.Where(p => !p.IsFolded && contributions[p] > 0).ToList();

        // semua level (termasuk kontribusi pemain yang fold) — urut naik
        var levels = contributions.Values
            .Where(v => v > 0)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        var pots = new List<(int amount, List<IPlayer> eligible)>();
        int prev = 0;

        foreach (int level in levels)
        {
            int potShare = 0;
            foreach (var p in _players)
            {
                int contributedAtLevel = Math.Min(contributions[p], level) - prev;
                if (contributedAtLevel > 0)
                    potShare += contributedAtLevel;
            }

            // eligible = pemain yang *masih di showdown* (tidak fold) dan kontribusinya >= level
            var eligibles = showdownPlayers.Where(p => contributions[p] >= level).ToList();

            if (potShare > 0 && eligibles.Count > 0)
                pots.Add((potShare, eligibles));

            prev = level;
        }

        _table.Pot.Clear();

        // helper lokal: tentukan pemenang terbaik di antara list pemain eligible
        List<IPlayer> DetermineWinnersAmong(List<IPlayer> eligibles)
        {
            if (eligibles == null || eligibles.Count == 0) return new List<IPlayer>();

            var results = eligibles
                .Select(p => (player: p, result: EvaluateHand(p.Hand.Cards.ToList(), _communityCards)))
                .ToList();

            int bestStrength = results.Max(r => r.result.Strength);
            var bestCandidates = results.Where(r => r.result.Strength == bestStrength).ToList();

            var chosen = new List<IPlayer>();
            var bestRes = bestCandidates.First().result;
            chosen.Add(bestCandidates.First().player);

            for (int i = 1; i < bestCandidates.Count; i++)
            {
                int cmp = CompareKickers(bestCandidates[i].result.Kickers, bestRes.Kickers);
                if (cmp > 0)
                {
                    chosen.Clear();
                    chosen.Add(bestCandidates[i].player);
                    bestRes = bestCandidates[i].result;
                }
                else if (cmp == 0)
                {
                    chosen.Add(bestCandidates[i].player);
                }
            }

            return chosen;
        }

        // Sekarang bagikan tiap pot berdasarkan eligibility + pemenang di antara eligibles
        foreach (var (amount, eligibles) in pots)
        {
            var potWinners = DetermineWinnersAmong(eligibles)
                .Where(w => winners.Contains(w) || true) // winners param is not needed here; we evaluate per-pot
                .ToList();

            if (potWinners.Count == 0)
                continue;

            int share = amount / potWinners.Count;
            int rem = amount % potWinners.Count;

            for (int i = 0; i < potWinners.Count; i++)
            {
                int give = share + (i == 0 ? rem : 0); 
                AddChipsToPlayerByAmount(potWinners[i], give);
                Console.WriteLine($"{potWinners[i].Name} receives {FormatChips(give)} from side pot.");
            }
        }
    }

   private void AddChipsToPlayerByAmount(IPlayer player, int amount)
    {
        int remaining = amount;
        var toAdd = new List<Chip>();
        while (remaining > 0)
        {
            if (remaining >= (int)ChipType.Black) { toAdd.Add(new Chip(ChipType.Black)); remaining -= (int)ChipType.Black; }
            else if (remaining >= (int)ChipType.Green) { toAdd.Add(new Chip(ChipType.Green)); remaining -= (int)ChipType.Green; }
            else if (remaining >= (int)ChipType.Red) { toAdd.Add(new Chip(ChipType.Red)); remaining -= (int)ChipType.Red; }
            else { toAdd.Add(new Chip(ChipType.White)); remaining -= (int)ChipType.White; }
        }
        player.Chips.AddRange(toAdd);
        NormalizeChips(player.Chips);

        // 🔥 update balance juga
        player.Balance += amount;
    }


    // utility: display table state
    public void ShowTableState()
    {
        Console.WriteLine("\n=== TABLE STATE ===");
        Console.WriteLine("Players:");
        foreach (var p in _players)
        {
            string chipText = FormatChips(p.Balance); // gunakan formatter chips
            Console.WriteLine($"- {p.Name} | Chips: {chipText} | Folded: {p.IsFolded}");
        }
        Console.WriteLine($"Pot value: {FormatChips(GetPotValue())}");
        ShowBoard();
        Console.WriteLine("===================\n");
    }
}


    



