using System;
using System.Collections.Generic;
using System.Linq;
using PokerGame.Backend.Interfaces;
using PokerGame.Backend.Models;

namespace PokerGame.Backend.Game
{
    public enum PlayerAction
    {
        Fold, Check, Call, Raise, AllIn
    }

    public enum GameEventType
    {
        GameStarted,
        RoundStart,
        RoundEnded,
        PlayerFolded,
        PlayerRaised,
        PlayerAllin,
        PlayerChecked,
        PlayerCalled,
        CardsDealt,
        CommunityDealt,
        Showdown,
        Winner
    }

    public class PokerGame
    {
        private readonly ITable _table;
        private readonly Random _random = new();
        private readonly List<ICard> _communityCards = new();
        private int _currentBet;
        private int _minRaise = 10;
        private int _bigBlind = 20;
        private int _smallBlindIndex = 0;
        private int _bigBlindIndex = 1;
        private int _totalPot = 0;
        public List<IPlayer> GetPlayers() => _players;
        public List<ICard> GetCommunityCards() => _communityCards;


        private readonly List<IPlayer> _players = new();

        // Event untuk komunikasi ke frontend
        public Action<GameEventType, IPlayer?>? OnGameEvent;
        public Action<IPlayer, string, int>? OnGameEnded;
        public Func<IPlayer, int, int, PlayerAction>? OnPlayerDecision;

        public PokerGame(ITable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
        }

        // ======= GAME FLOW =======
        public void StartGame()
        {
            OnGameEvent?.Invoke(GameEventType.GameStarted, null);

            if (_players.Count < 2)
                throw new InvalidOperationException("Minimal 2 pemain untuk memulai game");

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
            ResetDeck();
            ResetForNewRound();
            RotateBlinds();
            PostBlinds();
            DealCards();

            // Pre-flop
            BettingRound();
            if (CheckEndRound()) return;

            // Flop
            DealCommunityCards(3);
            BettingRound();
            if (CheckEndRound()) return;

            // Turn
            DealCommunityCards(1);
            BettingRound();
            if (CheckEndRound()) return;

            // River
            DealCommunityCards(1);
            BettingRound();
            if (CheckEndRound()) return;

            // Showdown
            Showdown();
            DistributePot();
            EndRound();
        }

        private bool CheckEndRound()
        {
            if (AllPlayersAllInOrFolded())
            {
                RevealRemainingCardsAndShowdown();
                EndRound();
                return true;
            }
            return false;
        }

        private void EndRound()
        {
            OnGameEvent?.Invoke(GameEventType.RoundEnded, null);
            _table.Pot.Clear();
            _totalPot = 0;

            var eliminated = _players
                .Where(p => p.Balance <= 0 || p.Balance < (int)ChipType.White)
                .ToList();

            foreach (var p in eliminated)
                RemovePlayer(p);
        }

        // ======= DEALING =======
        private void DealCards()
        {
            foreach (var p in _players)
            {
                var c1 = DealCardDeck();
                var c2 = DealCardDeck();
                ((Hand)p.Hand).AddCard(c1);
                ((Hand)p.Hand).AddCard(c2);

                OnGameEvent?.Invoke(GameEventType.CardsDealt, p);
            }
        }

        private void DealCommunityCards(int count)
        {
            for (int i = 0; i < count; i++)
                _communityCards.Add(DealCardDeck());

            OnGameEvent?.Invoke(GameEventType.CommunityDealt, null);
        }

        private void RevealRemainingCardsAndShowdown()
        {
            int remaining = 5 - _communityCards.Count;
            if (remaining > 0)
            {
                for (int i = 0; i < remaining; i++)
                    _communityCards.Add(DealCardDeck());
            }
            Showdown();
            DistributePot();
        }

        // ======= BETTING =======
        private void PostBlinds()
        {
            if (_players.Count < 2) return;

            var sb = _players[_smallBlindIndex];
            var bb = _players[_bigBlindIndex];

            int smallAmt = _bigBlind / 2;
            AddToPot(smallAmt);
            sb.Balance -= smallAmt;
            sb.CurrentBet = smallAmt;
            sb.TotalContributed += smallAmt;

            int bbAmt = Math.Min(_bigBlind, bb.Balance);
            AddToPot(bbAmt);
            bb.Balance -= bbAmt;
            bb.CurrentBet = bbAmt;
            bb.TotalContributed += bbAmt;

            _currentBet = _bigBlind;
        }

        private void BettingRound()
        {
            foreach (var p in _players.Where(x => !x.IsFolded && x.Balance > 0))
            {
                var action = OnPlayerDecision?.Invoke(p, _currentBet, _minRaise)
                             ?? PlayerAction.Check;
                ProcessPlayerAction(p, action);
            }
        }

        private void ProcessPlayerAction(IPlayer player, PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Fold:
                    player.IsFolded = true;
                    OnGameEvent?.Invoke(GameEventType.PlayerFolded, player);
                    break;

                case PlayerAction.Check:
                    OnGameEvent?.Invoke(GameEventType.PlayerChecked, player);
                    break;

                case PlayerAction.Call:
                    int toCall = _currentBet - player.CurrentBet;
                    int callAmt = Math.Min(toCall, player.Balance);
                    AddToPot(callAmt);
                    player.Balance -= callAmt;
                    player.CurrentBet += callAmt;
                    player.TotalContributed += callAmt;
                    OnGameEvent?.Invoke(GameEventType.PlayerCalled, player);
                    break;

                case PlayerAction.Raise:
                    int raiseAmt = Math.Min(player.Balance, _minRaise + (_currentBet - player.CurrentBet));
                    AddToPot(raiseAmt);
                    player.Balance -= raiseAmt;
                    player.CurrentBet += raiseAmt;
                    player.TotalContributed += raiseAmt;
                    _currentBet = player.CurrentBet;
                    OnGameEvent?.Invoke(GameEventType.PlayerRaised, player);
                    break;

                case PlayerAction.AllIn:
                    int allIn = player.Balance;
                    AddToPot(allIn);
                    player.Balance = 0;
                    player.CurrentBet += allIn;
                    player.TotalContributed += allIn;
                    if (player.CurrentBet > _currentBet)
                        _currentBet = player.CurrentBet;
                    OnGameEvent?.Invoke(GameEventType.PlayerAllin, player);
                    break;
            }
        }

        // ======= SHOWDOWN =======
       private void Showdown()
    {
        OnGameEvent?.Invoke(GameEventType.Showdown, null);

        var activePlayers = _players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count == 0) return;

        // Jika hanya 1 pemain tersisa → otomatis menang
        if (activePlayers.Count == 1)
        {
            var soleWinner = activePlayers.First();
            AddChipsToPlayer(soleWinner, _totalPot);
            OnGameEvent?.Invoke(GameEventType.Winner, soleWinner);
            _totalPot = 0;
            return;
        }

        // Evaluasi semua pemain
        var results = activePlayers
            .Select(p => (player: p, result: HandEvaluator.EvaluateHand(p.Hand.Cards.ToList(), _communityCards)))
            .ToList();

        int bestStrength = results.Max(r => r.result.Strength);
        var bestCandidates = results.Where(r => r.result.Strength == bestStrength).ToList();

        // Cari pemenang dengan bandingkan Kickers
        var winners = new List<IPlayer>();
        var bestResult = bestCandidates.First().result;
        winners.Add(bestCandidates.First().player);

        for (int i = 1; i < bestCandidates.Count; i++)
        {
            int cmp = HandEvaluator.CompareKickers(bestCandidates[i].result.Kickers, bestResult.Kickers);
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

        // Bagikan pot
        if (winners.Count == 1)
        {
            var winPlayer = winners.First();
            AddChipsToPlayer(winPlayer, _totalPot);
            OnGameEvent?.Invoke(GameEventType.Winner, winPlayer);
        }
        else
        {
            int share = _totalPot / winners.Count;
            foreach (var w in winners)
            {
                AddChipsToPlayer(w, share);
                OnGameEvent?.Invoke(GameEventType.Winner, w);
            }
        }

        _totalPot = 0;
    }


        private void DistributePot()
        {
            // sementara simple winner → semua pot
        }

        // ======= HELPERS =======
        private bool AllPlayersAllInOrFolded()
        {
            var active = _players.Where(p => !p.IsFolded).ToList();
            if (active.Count <= 1) return true;
            return active.All(p => p.Balance == 0);
        }

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
        }

        private void AddToPot(int amount)
        {
            _totalPot += amount;
            _table.Pot.Add(new Chip(ChipType.White)); // simplified
        }

        private void AddChipsToPlayer(IPlayer player, int amount)
        {
            player.Balance += amount;
        }

        private ICard DealCardDeck()
        {
            var deck = _table.Deck.Cards;
            var card = deck[0];
            deck.RemoveAt(0);
            return card;
        }

        private void ResetDeck()
        {
            _table.Deck.Initialize();
            _table.Deck.Shuffle(_random);
        }

        public void AddPlayer(string name, bool isAI)
        {
            IPlayer player = isAI ? new AIPlayer(name, 1000) : new HumanPlayer(name, 1000);
            _table.players.Add(player);
            _players.Add(player);
        }

        public void RemovePlayer(IPlayer player)
        {
            _table.players.Remove(player);
            _players.Remove(player);
        }

    }
}




