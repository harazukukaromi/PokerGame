using System;
using System.Collections.Generic;
using System.Linq;
using PokerGame.Backend.Interfaces;

namespace PokerGame.Backend.Game
{
    public class HandResult
    {
        public string Name { get; set; } = "High Card";
        public int Strength { get; set; } = 1;
        public List<ICard> Kickers { get; set; } = new();

        public string KickersAsString => string.Join(", ",
            Kickers.Select(c => $"{c.Rank} of {c.Suit}"));
    }

    public static class HandEvaluator
    {
        public static HandResult EvaluateHand(List<ICard> handCards, List<ICard> communityCards)
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

        private static HandResult EvaluateBestHand(List<ICard> cards)
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

        private static HandResult EvaluateFiveCardHand(List<ICard> cards)
        {
            var sorted = cards.OrderByDescending(c => c.Rank).ToList();
            var groups = cards.GroupBy(c => c.Rank)
                            .OrderByDescending(g => g.Count())
                            .ThenByDescending(g => g.Key)
                            .ToList();

            if (IsRoyalFlush(cards))
                return new HandResult { Name = "Royal Flush", Strength = GetHandStrength("Royal Flush"), Kickers = sorted };

            if (IsStraightFlush(cards))
                return new HandResult { Name = "Straight Flush", Strength = GetHandStrength("Straight Flush"), Kickers = sorted };

            if (IsFourOfAKind(cards))
            {
                var quad = groups.First(g => g.Count() == 4).ToList();
                var kicker = cards.Except(quad).OrderByDescending(c => c.Rank).First();
                return new HandResult { Name = "Four of a Kind", Strength = GetHandStrength("Four of a Kind"), Kickers = quad.Concat(new[] { kicker }).ToList() };
            }

            if (IsFullHouse(cards))
            {
                var three = groups.First(g => g.Count() == 3).ToList();
                var pair = groups.First(g => g.Count() == 2).ToList();
                return new HandResult { Name = "Full House", Strength = GetHandStrength("Full House"), Kickers = three.Concat(pair).ToList() };
            }

            if (IsFlush(cards))
            {
                var flushSuit = cards.GroupBy(c => c.Suit).First(g => g.Count() >= 5).Key;
                var flushCards = cards.Where(c => c.Suit == flushSuit).OrderByDescending(c => c.Rank).Take(5).ToList();
                return new HandResult { Name = "Flush", Strength = GetHandStrength("Flush"), Kickers = flushCards };
            }

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

            if (IsThreeOfAKind(cards))
            {
                var trips = groups.First(g => g.Count() == 3).ToList();
                var kickers = cards.Except(trips).OrderByDescending(c => c.Rank).Take(2).ToList();
                return new HandResult { Name = "Three of a Kind", Strength = GetHandStrength("Three of a Kind"), Kickers = trips.Concat(kickers).ToList() };
            }

            if (IsTwoPair(cards))
            {
                var pairs = groups.Where(g => g.Count() == 2).OrderByDescending(g => g.Key).Take(2).SelectMany(g => g).ToList();
                var kicker = cards.Except(pairs).OrderByDescending(c => c.Rank).First();
                return new HandResult { Name = "Two Pair", Strength = GetHandStrength("Two Pair"), Kickers = pairs.Concat(new[] { kicker }).ToList() };
            }

            if (IsOnePair(cards))
            {
                var pair = groups.First(g => g.Count() == 2).ToList();
                var kickers = cards.Except(pair).OrderByDescending(c => c.Rank).Take(3).ToList();
                return new HandResult { Name = "One Pair", Strength = GetHandStrength("One Pair"), Kickers = pair.Concat(kickers).ToList() };
            }

            return new HandResult { Name = $"High Card {sorted.First().Rank}", Strength = GetHandStrength("High Card"), Kickers = sorted.Take(5).ToList() };
        }

        private static IEnumerable<List<ICard>> GetCombinations(List<ICard> cards, int k)
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

        public static int GetHandStrength(string hand) => hand switch
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

        public static int CompareKickers(List<ICard> k1, List<ICard> k2)
        {
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

        private static bool IsRoyalFlush(List<ICard> cards) =>
            IsStraightFlush(cards) && cards.Any(c => c.Rank == Rank.Ace) && cards.Any(c => c.Rank == Rank.King);

        private static bool IsStraightFlush(List<ICard> cards) =>
            IsFlush(cards) && IsStraight(cards);

        private static bool IsFourOfAKind(List<ICard> cards) =>
            cards.GroupBy(c => c.Rank).Any(g => g.Count() == 4);

        private static bool IsFullHouse(List<ICard> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).ToList();
            return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
        }

        private static bool IsFlush(List<ICard> cards) =>
            cards.GroupBy(c => c.Suit).Any(g => g.Count() >= 5);

        private static bool IsStraight(List<ICard> cards)
        {
            var ranks = cards.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();
            if (ranks.Contains((int)Rank.Ace))
                ranks.Insert(0, 1); // Ace low

            int consecutive = 1;
            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] == ranks[i - 1] + 1)
                {
                    consecutive++;
                    if (consecutive >= 5) return true;
                }
                else consecutive = 1;
            }
            return false;
        }

        private static bool IsThreeOfAKind(List<ICard> cards) =>
            cards.GroupBy(c => c.Rank).Any(g => g.Count() == 3);

        private static bool IsTwoPair(List<ICard> cards) =>
            cards.GroupBy(c => c.Rank).Count(g => g.Count() == 2) >= 2;

        private static bool IsOnePair(List<ICard> cards) =>
            cards.GroupBy(c => c.Rank).Any(g => g.Count() == 2);
    }
}
