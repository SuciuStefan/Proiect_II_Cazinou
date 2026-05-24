// CasinoApp.BusinessLogic/Services/FlipACoinService.cs
//
// All Flip-a-Coin game logic extracted from FlipACoin.razor.
// Includes the streak/doubling accumulator logic.
//
// Register in Program.cs:
//   builder.Services.AddScoped<IFlipACoinService, FlipACoinService>();

using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public interface IFlipACoinService
    {
        // Betting state
        decimal CurrentBet { get; }
        decimal LastBet    { get; }
        bool    IsStreak   { get; }

        // Bet mutations
        void    AddChip(int denomination, double playerBalance, out string? error);
        void    ClearBet();
        void    Rebet(double playerBalance, out string? error);

        // Flip gate check — Razor calls this before allowing the button
        bool CanFlip(double playerBalance);

        // Core flip — returns everything the Razor needs to update UI + DB
        FlipResult Flip(double balanceAfterBetDeducted, string playerChoice);

        // Called when player collects streak winnings manually (ClearBet on a streak)
        CollectResult CollectStreak(double currentBalance);

        // UI helper
        string GetResultClass(decimal resultWin);

        // Chip stack display helper
        List<List<int>> GetChipColumns(int amount);
    }

    // Returned by Flip() — Razor uses these to update DB and drive UI
    public record FlipResult(
        string  CoinResult,       // "heads" | "tails"
        string  CoinClass,        // CSS class for the coin after animation
        bool    Won,
        string  ResultMsg,
        decimal ResultWin,        // positive = profit this flip, negative = loss
        decimal NewCurrentBet,    // bet to show on table after flip (doubled if streak, 0 if lost)
        decimal LastBet,
        bool    IsStreak,         // true = pot stays on table, doubled
        bool    LimitReached,     // true = pot hit 10 000 RON cap, auto-collected
        // DB actions the Razor must perform:
        bool    ShouldDeductBet,  // false during a streak flip (already deducted earlier)
        bool    ShouldCreditWin,  // true when limit reached — credit doubled pot immediately
        double  CreditAmount,     // amount to add to balance when ShouldCreditWin is true
        string  BetStatus         // "Won" | "Lost" | "Pending" (streak continues)
    );

    public record CollectResult(
        double NewBalance,
        string BetStatus   // always "Won"
    );

    public class FlipACoinService : IFlipACoinService
    {
        private readonly Random _rng = new();

        public decimal CurrentBet { get; private set; } = 0;
        public decimal LastBet    { get; private set; } = 0;
        public bool    IsStreak   { get; private set; } = false;

        // ── Betting ───────────────────────────────────────────────────────────

        public void AddChip(int denomination, double playerBalance, out string? error)
        {
            error = null;
            if (IsStreak)
            {
                error = "Ești pe dublaj! Dă FLIP sau apasă pe încasare.";
                return;
            }
            if (playerBalance < (double)(CurrentBet + denomination))
            {
                error = "Balanta insuficienta!";
                return;
            }
            CurrentBet += denomination;
        }

        public void ClearBet()
        {
            // Note: streak collection (crediting balance) is handled by CollectStreak().
            // ClearBet only zeros out the local state.
            CurrentBet = 0;
            IsStreak   = false;
        }

        public void Rebet(double playerBalance, out string? error)
        {
            error = null;
            if (IsStreak) { error = ""; return; } // silently blocked — button is disabled anyway
            if (LastBet == 0 || playerBalance < (double)LastBet) return;
            CurrentBet = LastBet;
        }

        public bool CanFlip(double playerBalance) =>
            CurrentBet > 0 &&
            (IsStreak || playerBalance >= (double)CurrentBet);
        // Note: playerChoice validation stays in the Razor (it's pure UI state)

        // ── Core flip ─────────────────────────────────────────────────────────
        // balanceAfterBetDeducted: pass Session.CurrentPlayer.Balance AFTER the
        // bet has been deducted (or the current balance if this is a streak flip,
        // since no deduction happens on streak flips).
        

        public FlipResult Flip(double balanceAfterBetDeducted, string playerChoice)
        {
            string coinResult = _rng.Next(2) == 0 ? "heads" : "tails";
            string coinClass  = coinResult == "heads" ? "show-heads" : "show-tails";
            bool   won        = coinResult == playerChoice;

            bool    limitReached    = false;
            bool    shouldCredit    = false;
            double  creditAmount    = 0;
            string  resultMsg;
            decimal resultWin;
            string  betStatus;

            if (won)
            {
                decimal winAmt      = Math.Round(CurrentBet, 2);
                resultMsg           = coinResult == "heads" ? "★ HEADS — AI CASTIGAT! ★" : "◈ TAILS — AI CASTIGAT! ◈";
                resultWin           = winAmt;

                decimal doubledPot  = CurrentBet * 2;

                if (doubledPot >= 10_000m)
                {
                    // Auto-collect: pot hit the cap
                    resultMsg    += " LIMITĂ ATINSĂ!";
                    limitReached  = true;
                    shouldCredit  = true;
                    creditAmount  = (double)doubledPot;
                    betStatus     = "Won";

                    LastBet    = CurrentBet;
                    CurrentBet = 0;
                    IsStreak   = false;
                }
                else
                {
                    // Keep doubled pot on table — streak continues
                    betStatus  = "Pending";
                    LastBet    = CurrentBet;
                    CurrentBet = doubledPot;
                    IsStreak   = true;
                }
            }
            else
            {
                resultMsg  = coinResult == "heads" ? "HEADS — AI PIERDUT" : "TAILS — AI PIERDUT";
                resultWin  = -CurrentBet;
                betStatus  = "Lost";

                LastBet    = CurrentBet;
                CurrentBet = 0;
                IsStreak   = false;
            }

            return new FlipResult(
                CoinResult:       coinResult,
                CoinClass:        coinClass,
                Won:              won,
                ResultMsg:        resultMsg,
                ResultWin:        resultWin,
                NewCurrentBet:    CurrentBet,
                LastBet:          LastBet,
                IsStreak:         IsStreak,
                LimitReached:     limitReached,
                ShouldDeductBet:  false,  // Razor already deducted before calling Flip()
                ShouldCreditWin:  shouldCredit,
                CreditAmount:     creditAmount,
                BetStatus:        betStatus
            );
        }

        public CollectResult CollectStreak(double currentBalance)
        {
            double newBalance = currentBalance + (double)CurrentBet;
            ClearBet();
            return new CollectResult(newBalance, "Won");
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        public string GetResultClass(decimal resultWin) =>
            resultWin > 0 ? "fc-res-win" : "fc-res-lose";

        public List<List<int>> GetChipColumns(int amount)
        {
            var columns   = new List<List<int>>();
            int remaining = amount;
            foreach (var d in new[] { 100, 50, 25, 10, 5, 1 })
            {
                if (remaining <= 0 || columns.Count >= 10) break;
                int count = remaining / d;
                if (count == 0) continue;
                remaining -= count * d;
                while (count > 0 && columns.Count < 10)
                {
                    int take = Math.Min(count, 10);
                    columns.Add(Enumerable.Repeat(d, take).ToList());
                    count -= take;
                }
            }
            return columns;
        }
    }
}
