using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public record CardOption(int Cost, string Name, string Emoji, int Jackpot);
    public record ScratchSymbol(string Emoji, double Multiplier);

    public record ScratchResult(
        double TotalWin,
        List<int> WinningRows,
        string BetStatus
    );

    public interface IScratchCardService
    {
        IReadOnlyList<CardOption> CardOptions { get; }
        IReadOnlyList<ScratchSymbol> Symbols { get; }

        string[] Grid { get; }
        int SelectedCost { get; }

        void SelectCard(int cost, double playerBalance);

        BuyResult BuyCard(double playerBalance, out string? error);

        ScratchResult EvaluateGrid();

        string GetRowSymbol(int row);
        double GetRowWin(int row);

        void Reset();
    }

    public record BuyResult(
        double NewBalance,
        int Cost
    );

    public class ScratchCardService : IScratchCardService
    {
        public IReadOnlyList<CardOption> CardOptions { get; } = new List<CardOption>
        {
            new(5,  "BRONZ",  "🥉", 250),
            new(10, "ARGINT", "🥈", 1000),
            new(50, "AUR",    "🥇", 10000),
        };

        public IReadOnlyList<ScratchSymbol> Symbols { get; } = new List<ScratchSymbol>
        {
            new("🍒", 1.5),
            new("🍋", 2.0),
            new("🔔", 4.0),
            new("⭐", 8.0),
            new("💎", 20.0),
            new("7️⃣", 50.0),
        };

        public string[] Grid { get; private set; } = new string[9];
        public int SelectedCost { get; private set; } = 0;

        private readonly Random _rng = new();

        public void SelectCard(int cost, double playerBalance)
        {
            if (playerBalance < cost) return;
            SelectedCost = cost;
        }

        public BuyResult BuyCard(double playerBalance, out string? error)
        {
            error = null;

            if (SelectedCost == 0)
            {
                error = "Alege un bilet mai întâi!";
                return null!;
            }
            if (playerBalance < SelectedCost)
            {
                error = "Balanță insuficientă!";
                return null!;
            }

            GenerateGrid();

            return new BuyResult(
                NewBalance: playerBalance - SelectedCost,
                Cost: SelectedCost
            );
        }

        public ScratchResult EvaluateGrid()
        {
            var winningRows = new List<int>();
            double totalWin = 0;

            for (int row = 0; row < 3; row++)
            {
                string a = Grid[row * 3];
                string b = Grid[row * 3 + 1];
                string c = Grid[row * 3 + 2];

                if (a == b && b == c)
                {
                    winningRows.Add(row);
                    var sym = Symbols.First(s => s.Emoji == a);
                    totalWin += sym.Multiplier * SelectedCost;
                }
            }

            return new ScratchResult(
                TotalWin: totalWin,
                WinningRows: winningRows,
                BetStatus: totalWin > 0 ? "Won" : "Lost"
            );
        }

        public string GetRowSymbol(int row) => Grid[row * 3];

        public double GetRowWin(int row)
        {
            var sym = Symbols.FirstOrDefault(s => s.Emoji == Grid[row * 3]);
            return sym?.Multiplier * SelectedCost ?? 0;
        }

        public void Reset()
        {
            SelectedCost = 0;
            Grid = new string[9];
        }

        private void GenerateGrid()
        {
            Grid = new string[9];

            double winChance = SelectedCost switch { 5 => 0.32, 10 => 0.38, 50 => 0.45, _ => 0.33 };

            int winCount = _rng.NextDouble() < winChance
                ? (_rng.NextDouble() < 0.08 ? 2 : _rng.NextDouble() < 0.01 ? 3 : 1)
                : 0;

            var rows = new List<int> { 0, 1, 2 };
            for (int i = rows.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (rows[i], rows[j]) = (rows[j], rows[i]);
            }
            var winRows = new HashSet<int>(rows.Take(winCount));

            for (int row = 0; row < 3; row++)
            {
                if (winRows.Contains(row))
                {
                    var sym = PickWinningSymbol();
                    Grid[row * 3] = sym.Emoji;
                    Grid[row * 3 + 1] = sym.Emoji;
                    Grid[row * 3 + 2] = sym.Emoji;
                }
                else
                {
                    string a, b, c;
                    do
                    {
                        a = Symbols[_rng.Next(Symbols.Count)].Emoji;
                        b = Symbols[_rng.Next(Symbols.Count)].Emoji;
                        c = Symbols[_rng.Next(Symbols.Count)].Emoji;
                    } while (a == b && b == c);

                    Grid[row * 3] = a;
                    Grid[row * 3 + 1] = b;
                    Grid[row * 3 + 2] = c;
                }
            }
        }

        private ScratchSymbol PickWinningSymbol()
        {
            int[] weights = SelectedCost switch
            {
                50 => new[] { 40, 24, 16, 11, 7, 2 },
                10 => new[] { 46, 25, 14, 9, 5, 1 },
                _ => new[] { 50, 26, 13, 7, 3, 1 },
            };

            int total = weights.Sum();
            int pick = _rng.Next(total);
            int cum = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                cum += weights[i];
                if (pick < cum) return Symbols[i];
            }
            return Symbols[0];
        }
    }
}