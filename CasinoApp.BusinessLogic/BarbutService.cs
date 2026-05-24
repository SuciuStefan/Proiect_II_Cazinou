// CasinoApp.BusinessLogic/Services/BarbutService.cs
//
// All Barbut game logic extracted from Barbut.razor.
// Zero Blazor/UI dependencies — returns plain data objects.
//
// Register in Program.cs:
//   builder.Services.AddScoped<IBarbutService, BarbutService>();

using System;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public interface IBarbutService
    {
        // State
        int[]   PlayerDice { get; }
        int[][] AIDice     { get; }
        int     PlayerSum  { get; }
        int[]   AISums     { get; }
        int     BeatCount  { get; }
        double  NetGain    { get; }

        // AI cosmetics (names/avatars are UI-only but generated here for testability)
        string[] AINames   { get; }
        string[] AIAvatars { get; }

        // Called once on component init
        void PickRandomAINames();

        // Called when the player clicks Roll — returns balance after bet deduction
        RollSetup PrepareRoll(int betAmount, int diceCount, double playerBalance);

        // Called by JS callback OnRollComplete — returns new balance if player won
        RollResult ResolveRoll(int betAmount, double balanceAfterBet);

        // UI helpers (CSS class strings — kept here so Razor stays logic-free)
        string GetPlayerCardClass();
        string GetPlayerSumClass();
        string GetAICardClass(int idx);
        string GetAISumClass(int idx);
        string GetAIVerdictClass(int idx);
        string GetAIVerdictText(int idx);
        string BuildDieDots(int value);
    }

    // PrepareRoll return: everything the Razor needs before calling JS
    public record RollSetup(
        int[]   PlayerDice,
        int[][] AIDice,
        int     PlayerSum,
        int[]   AISums,
        int     BeatCount,
        double  NetGain,
        double  BalanceAfterBet
    );

    // ResolveRoll return: final balance (only changes if player won)
    public record RollResult(
        double  NewBalance,
        bool    BalanceChanged,   // false if player lost — no DB write needed
        string  BetStatus         // "Won" | "Push" | "Lost"
    );

    public class BarbutService : IBarbutService
    {
        // ── Name/avatar pools ─────────────────────────────────────────────────
        private static readonly string[] NamePool =
        {
            "Andrei", "Mihai", "Ion", "Alexandru", "Bogdan",
            "Cristian", "Daniel", "Eduard", "Florin", "George",
            "Horia", "Ionuț", "Liviu", "Marcel", "Nicolae",
            "Octavian", "Pavel", "Radu", "Silviu", "Tudor",
            "Valentin", "Victor", "Vlad", "Cătălin", "Călin",
            "Dragoș", "Lucian", "Marian", "Sorin", "Cosmin"
        };

        private static readonly string[] AvatarPool =
        {
            "🎭", "🤠", "😈", "🥸", "😎", "🤑", "🧠", "👹",
            "🤡", "😤", "🫡", "🥷", "🤯", "😏", "💀"
        };

        // ── State ─────────────────────────────────────────────────────────────
        public int[]   PlayerDice { get; private set; } = Array.Empty<int>();
        public int[][] AIDice     { get; private set; } = { Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>() };
        public int     PlayerSum  { get; private set; }
        public int[]   AISums     { get; private set; } = new int[3];
        public int     BeatCount  { get; private set; }
        public double  NetGain    { get; private set; }

        public string[] AINames   { get; private set; } = new string[3];
        public string[] AIAvatars { get; private set; } = new string[3];

        // ── Init ──────────────────────────────────────────────────────────────
        public void PickRandomAINames()
        {
            var rng   = new Random();
            AINames   = NamePool.OrderBy(_ => rng.Next()).Take(3).ToArray();
            AIAvatars = AvatarPool.OrderBy(_ => rng.Next()).Take(3).ToArray();
        }

        // ── Roll logic ────────────────────────────────────────────────────────
        public RollSetup PrepareRoll(int betAmount, int diceCount, double playerBalance)
        {
            var rng  = new Random();

            PlayerDice = Enumerable.Range(0, diceCount).Select(_ => rng.Next(1, 7)).ToArray();
            for (int i = 0; i < 3; i++)
                AIDice[i] = Enumerable.Range(0, diceCount).Select(_ => rng.Next(1, 7)).ToArray();

            PlayerSum = PlayerDice.Sum();
            for (int i = 0; i < 3; i++)
                AISums[i] = AIDice[i].Sum();

            BeatCount = AISums.Count(s => PlayerSum > s);

            double returns = BeatCount switch
            {
                3 => betAmount * 3,
                2 => betAmount * 2,
                1 => betAmount,
                _ => 0
            };
            NetGain = returns - betAmount;

            double balanceAfterBet = playerBalance - betAmount;

            return new RollSetup(PlayerDice, AIDice, PlayerSum, AISums, BeatCount, NetGain, balanceAfterBet);
        }

        public RollResult ResolveRoll(int betAmount, double balanceAfterBet)
        {
            double returns = NetGain + betAmount;
            bool   won     = returns > 0;

            double newBalance     = won ? balanceAfterBet + returns : balanceAfterBet;
            string betStatus      = NetGain > 0 ? "Won" : NetGain == 0 ? "Push" : "Lost";

            return new RollResult(newBalance, won || NetGain == 0, betStatus);
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        public string GetPlayerCardClass() => BeatCount switch
        {
            3 => "card-winner",
            0 => "card-loser",
            _ => "card-neutral"
        };

        public string GetPlayerSumClass() =>
            BeatCount == 3 ? "sum-win" : BeatCount == 0 ? "sum-lose" : "sum-push";

        public string GetAICardClass(int idx)
        {
            if (PlayerSum < AISums[idx]) return "card-winner";
            if (PlayerSum > AISums[idx]) return "card-loser";
            return "card-neutral";
        }

        public string GetAISumClass(int idx) =>
            PlayerSum < AISums[idx] ? "sum-win" : PlayerSum > AISums[idx] ? "sum-lose" : "sum-push";

        public string GetAIVerdictClass(int idx) =>
            PlayerSum < AISums[idx] ? "verdict-win" : PlayerSum > AISums[idx] ? "verdict-lose" : "verdict-push";

        public string GetAIVerdictText(int idx) =>
            PlayerSum < AISums[idx] ? "A BĂTUT JUCĂTORUL" :
            PlayerSum > AISums[idx] ? "BĂTUT DE JUCĂTOR" : "EGALITATE";

        public string BuildDieDots(int value)
        {
            var dots = value switch
            {
                1 => new[] { "mc" },
                2 => new[] { "tr", "bl" },
                3 => new[] { "tr", "mc", "bl" },
                4 => new[] { "tl", "tr", "bl", "br" },
                5 => new[] { "tl", "tr", "mc", "bl", "br" },
                6 => new[] { "tl", "tr", "ml", "mr", "bl", "br" },
                _ => Array.Empty<string>()
            };
            return string.Concat(dots.Select(p => $"<span class='dot dot-{p}'></span>"));
        }
    }
}
