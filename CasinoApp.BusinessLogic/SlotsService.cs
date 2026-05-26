using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public record SymbolDef(string Emoji, int Weight, double Mult3, double Mult4, double Mult5);
    public record WinLine(string LineName, string Symbol, double Amount, int PaylineIndex, int MatchCount);

    public record SlotsSpinSetup(
        string[] FlatGrid,
        double BalanceAfterBet,
        int BetAmount
    );

    public record SlotsResult(
        List<WinLine> WinLines,
        List<int> ActivePaylines,
        HashSet<int> WinCells,
        double TotalWin,
        string BetStatus
    );

    public interface ISlotsService
    {
        IReadOnlyList<SymbolDef> Symbols { get; }

        string[,] ResultGrid { get; }
        string[,] DisplayGrid { get; }
        void CopyResultToDisplay();

        SlotsSpinSetup GenerateSpin(int betAmount, double playerBalance, out string? error);
        SlotsResult CalculateWins(int betAmount);

        void InitDisplayGrid();
    }

    public class SlotsService : ISlotsService
    {
        public IReadOnlyList<SymbolDef> Symbols { get; } = new List<SymbolDef>
        {
            new("🍒", 32, 3,  5,  10),
            new("🍋", 28, 3,  5,  10),
            new("🍊", 24, 3,  5,  10),
            new("⭐",  6, 2,  5,  15),
            new("🔔", 12, 5,  10, 25),
            new("💎",  2, 10, 25, 50),
            new("7️⃣",  1, 25, 100, 200),
        };

        private static readonly int[][] Paylines =
        {
            new[] { 0, 0, 0, 0, 0 },
            new[] { 1, 1, 1, 1, 1 },
            new[] { 2, 2, 2, 2, 2 },
            new[] { 2, 1, 0, 1, 2 },
            new[] { 0, 1, 2, 1, 0 },
        };

        private static readonly string[] PaylineNames =
            { "P1 Sus", "P2 Mijloc", "P3 Jos", "P4 V-shape", "P5 Λ-shape" };

        public string[,] ResultGrid { get; private set; } = new string[5, 3];
        public string[,] DisplayGrid { get; private set; } = new string[5, 3];

        private readonly Random _rng = new();

        public void InitDisplayGrid()
        {
            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    DisplayGrid[r, row] = Symbols[_rng.Next(Symbols.Count)].Emoji;
        }

        public SlotsSpinSetup GenerateSpin(int betAmount, double playerBalance, out string? error)
        {
            error = null;
            if (playerBalance < betAmount) { error = "Balantă insuficientă!"; return null!; }

            GenerateResultGrid();

            var flat = new string[15];
            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    flat[r * 3 + row] = ResultGrid[r, row];

            return new SlotsSpinSetup(flat, playerBalance - betAmount, betAmount);
        }

        public void CopyResultToDisplay()
        {
            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    DisplayGrid[r, row] = ResultGrid[r, row];
        }

        public SlotsResult CalculateWins(int betAmount)
        {
            var winLines = new List<WinLine>();
            var activePaylines = new List<int>();
            var winCells = new HashSet<int>();
            double totalWin = 0;

            for (int pi = 0; pi < Paylines.Length; pi++)
            {
                var line = Paylines[pi];
                string first = ResultGrid[0, line[0]];

                int matchCount = 1;
                for (int r = 1; r < 5; r++)
                {
                    if (ResultGrid[r, line[r]] == first) matchCount++;
                    else break;
                }

                if (matchCount >= 3)
                {
                    var sym = Symbols.First(s => s.Emoji == first);
                    double mult = matchCount switch { 5 => sym.Mult5, 4 => sym.Mult4, _ => sym.Mult3 };
                    double win = mult * betAmount;

                    totalWin += win;
                    activePaylines.Add(pi);
                    winLines.Add(new WinLine(PaylineNames[pi], first, win, pi, matchCount));

                    for (int r = 0; r < matchCount; r++)
                        winCells.Add(r * 3 + line[r]);
                }
            }

            return new SlotsResult(
                WinLines: winLines,
                ActivePaylines: activePaylines,
                WinCells: winCells,
                TotalWin: totalWin,
                BetStatus: totalWin > 0 ? "Won" : "Lost"
            );
        }

        private void GenerateResultGrid()
        {
            ResultGrid = new string[5, 3];

            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    ResultGrid[r, row] = PickSymbol();

            double roll = _rng.NextDouble();

            if (roll < 0.30)
            {
                int line = _rng.Next(Paylines.Length);
                string sym = PickSymbol();
                for (int r = 0; r < 3; r++) ResultGrid[r, Paylines[line][r]] = sym;
            }
            if (roll < 0.20)
            {
                int line = _rng.Next(Paylines.Length);
                string sym = PickSymbol();
                for (int r = 0; r < 4; r++) ResultGrid[r, Paylines[line][r]] = sym;
            }
            if (roll < 0.10)
            {
                int line = _rng.Next(Paylines.Length);
                string sym = PickSymbol();
                for (int r = 0; r < 5; r++) ResultGrid[r, Paylines[line][r]] = sym;
            }
        }

        private string PickSymbol()
        {
            int total = Symbols.Sum(s => s.Weight);
            int pick = _rng.Next(total);
            int cum = 0;
            foreach (var s in Symbols) { cum += s.Weight; if (pick < cum) return s.Emoji; }
            return Symbols[0].Emoji;
        }
    }
}