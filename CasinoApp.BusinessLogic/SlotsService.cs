// CasinoApp.BusinessLogic/Services/SlotsService.cs
//
// All Slots game logic extracted from Slots.razor.
// Symbol definitions, paylines, grid generation, win calculation.
//
// Register in Program.cs:
//   builder.Services.AddScoped<ISlotsService, SlotsService>();

using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    // ── Public data types ─────────────────────────────────────────────────────
    public record SymbolDef(string Emoji, int Weight, double Mult3, double Mult4, double Mult5);
    public record WinLine(string LineName, string Symbol, double Amount, int PaylineIndex, int MatchCount);

    public record SlotsSpinSetup(
        string[] FlatGrid,          // 15 elements [reel0row0, reel0row1, reel0row2, reel1row0 ...] sent to JS
        double   BalanceAfterBet,
        int      BetAmount
    );

    public record SlotsResult(
        List<WinLine>  WinLines,
        List<int>      ActivePaylines,
        HashSet<int>   WinCells,
        double         TotalWin,
        string         BetStatus      // "Won" | "Lost"
    );

    public interface ISlotsService
    {
        // ── Static data for paytable rendering ───────────────────────────────
        IReadOnlyList<SymbolDef> Symbols { get; }

        // ── Round state ───────────────────────────────────────────────────────
        string[,] ResultGrid  { get; }   // [reel, row] — set by GenerateSpin
        string[,] DisplayGrid { get; }   // shown in UI; updated after JS animation
        void CopyResultToDisplay();       // Razor calls this in OnSpinComplete

        // ── Round flow ────────────────────────────────────────────────────────
        SlotsSpinSetup GenerateSpin(int betAmount, double playerBalance, out string? error);
        SlotsResult CalculateWins(int betAmount);

        // ── Display init ──────────────────────────────────────────────────────
        void InitDisplayGrid();          // fills display with random symbols on first render
    }

    public class SlotsService : ISlotsService
    {
        // ── Symbol table ──────────────────────────────────────────────────────
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

        // ── Paylines ──────────────────────────────────────────────────────────
        // Each row value = which grid row (0=top,1=mid,2=bot) to check on that reel
        private static readonly int[][] Paylines =
        {
            new[] { 0, 0, 0, 0, 0 },   // P1 – middle row  (note: original code rows are 0-indexed top=0)
            new[] { 1, 1, 1, 1, 1 },   // P2 – top row
            new[] { 2, 2, 2, 2, 2 },   // P3 – bottom row
            new[] { 2, 1, 0, 1, 2 },   // P4 – V shape
            new[] { 0, 1, 2, 1, 0 },   // P5 – Λ shape
        };

        private static readonly string[] PaylineNames =
            { "P1 Sus", "P2 Mijloc", "P3 Jos", "P4 V-shape", "P5 Λ-shape" };

        // ── State ─────────────────────────────────────────────────────────────
        public string[,] ResultGrid  { get; private set; } = new string[5, 3];
        public string[,] DisplayGrid { get; private set; } = new string[5, 3];

        private readonly Random _rng = new();

        // ── Init ──────────────────────────────────────────────────────────────
        public void InitDisplayGrid()
        {
            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    DisplayGrid[r, row] = Symbols[_rng.Next(Symbols.Count)].Emoji;
        }

        // ── Spin ──────────────────────────────────────────────────────────────
        public SlotsSpinSetup GenerateSpin(int betAmount, double playerBalance, out string? error)
        {
            error = null;
            if (playerBalance < betAmount) { error = "Balantă insuficientă!"; return null!; }

            GenerateResultGrid();

            // Flatten to 1D array for JS: [r0c0, r0c1, r0c2, r1c0 ...] = 15 elements
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

        // ── Win evaluation ────────────────────────────────────────────────────
        public SlotsResult CalculateWins(int betAmount)
        {
            var winLines       = new List<WinLine>();
            var activePaylines = new List<int>();
            var winCells       = new HashSet<int>();
            double totalWin    = 0;

            for (int pi = 0; pi < Paylines.Length; pi++)
            {
                var line   = Paylines[pi];
                string first = ResultGrid[0, line[0]];

                int matchCount = 1;
                for (int r = 1; r < 5; r++)
                {
                    if (ResultGrid[r, line[r]] == first) matchCount++;
                    else break;
                }

                if (matchCount >= 3)
                {
                    var sym   = Symbols.First(s => s.Emoji == first);
                    double mult = matchCount switch { 5 => sym.Mult5, 4 => sym.Mult4, _ => sym.Mult3 };
                    double win  = mult * betAmount;

                    totalWin += win;
                    activePaylines.Add(pi);
                    winLines.Add(new WinLine(PaylineNames[pi], first, win, pi, matchCount));

                    for (int r = 0; r < matchCount; r++)
                        winCells.Add(r * 3 + line[r]);
                }
            }

            return new SlotsResult(
                WinLines:       winLines,
                ActivePaylines: activePaylines,
                WinCells:       winCells,
                TotalWin:       totalWin,
                BetStatus:      totalWin > 0 ? "Won" : "Lost"
            );
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void GenerateResultGrid()
        {
            ResultGrid = new string[5, 3];

            for (int r = 0; r < 5; r++)
                for (int row = 0; row < 3; row++)
                    ResultGrid[r, row] = PickSymbol();

            // Force win lines based on rolled probability
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
            int pick  = _rng.Next(total);
            int cum   = 0;
            foreach (var s in Symbols) { cum += s.Weight; if (pick < cum) return s.Emoji; }
            return Symbols[0].Emoji;
        }
    }
}
