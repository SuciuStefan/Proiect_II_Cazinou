using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public interface IFlipACoinService
    {
        decimal CurrentBet { get; }
        decimal LastBet { get; }
        bool IsStreak { get; }

        void AddChip(int denomination, double playerBalance, out string? error);
        void ClearBet();
        void Rebet(double playerBalance, out string? error);

        bool CanFlip(double playerBalance);

        FlipResult Flip(double balanceAfterBetDeducted, string playerChoice);

        CollectResult CollectStreak(double currentBalance);

        string GetResultClass(decimal resultWin);

        List<List<int>> GetChipColumns(int amount);
    }

    public record FlipResult(
        string CoinResult,
        string CoinClass,
        bool Won,
        string ResultMsg,
        decimal ResultWin,
        decimal NewCurrentBet,
        decimal LastBet,
        bool IsStreak,
        bool LimitReached,
        bool ShouldDeductBet,
        bool ShouldCreditWin,
        double CreditAmount,
        string BetStatus
    );

    public record CollectResult(
        double NewBalance,
        string BetStatus
    );

    public class FlipACoinService : IFlipACoinService
    {
        private readonly Random _rng = new();

        public decimal CurrentBet { get; private set; } = 0;
        public decimal LastBet { get; private set; } = 0;
        public bool IsStreak { get; private set; } = false;

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
            CurrentBet = 0;
            IsStreak = false;
        }

        public void Rebet(double playerBalance, out string? error)
        {
            error = null;
            if (IsStreak) { error = ""; return; }
            if (LastBet == 0 || playerBalance < (double)LastBet) return;
            CurrentBet = LastBet;
        }

        public bool CanFlip(double playerBalance) =>
            CurrentBet > 0 &&
            (IsStreak || playerBalance >= (double)CurrentBet);

        public FlipResult Flip(double balanceAfterBetDeducted, string playerChoice)
        {
            string coinResult = _rng.Next(2) == 0 ? "heads" : "tails";
            string coinClass = coinResult == "heads" ? "show-heads" : "show-tails";
            bool won = coinResult == playerChoice;

            bool limitReached = false;
            bool shouldCredit = false;
            double creditAmount = 0;
            string resultMsg;
            decimal resultWin;
            string betStatus;

            if (won)
            {
                decimal winAmt = Math.Round(CurrentBet, 2);
                resultMsg = coinResult == "heads" ? "★ HEADS — AI CASTIGAT! ★" : "◈ TAILS — AI CASTIGAT! ◈";
                resultWin = winAmt;

                decimal doubledPot = CurrentBet * 2;

                if (doubledPot >= 10_000m)
                {
                    resultMsg += " LIMITĂ ATINSĂ!";
                    limitReached = true;
                    shouldCredit = true;
                    creditAmount = (double)doubledPot;
                    betStatus = "Won";

                    LastBet = CurrentBet;
                    CurrentBet = 0;
                    IsStreak = false;
                }
                else
                {
                    betStatus = "Pending";
                    LastBet = CurrentBet;
                    CurrentBet = doubledPot;
                    IsStreak = true;
                }
            }
            else
            {
                resultMsg = coinResult == "heads" ? "HEADS — AI PIERDUT" : "TAILS — AI PIERDUT";
                resultWin = -CurrentBet;
                betStatus = "Lost";

                LastBet = CurrentBet;
                CurrentBet = 0;
                IsStreak = false;
            }

            return new FlipResult(
                CoinResult: coinResult,
                CoinClass: coinClass,
                Won: won,
                ResultMsg: resultMsg,
                ResultWin: resultWin,
                NewCurrentBet: CurrentBet,
                LastBet: LastBet,
                IsStreak: IsStreak,
                LimitReached: limitReached,
                ShouldDeductBet: false,
                ShouldCreditWin: shouldCredit,
                CreditAmount: creditAmount,
                BetStatus: betStatus
            );
        }

        public CollectResult CollectStreak(double currentBalance)
        {
            double newBalance = currentBalance + (double)CurrentBet;
            ClearBet();
            return new CollectResult(newBalance, "Won");
        }

        public string GetResultClass(decimal resultWin) =>
            resultWin > 0 ? "fc-res-win" : "fc-res-lose";

        public List<List<int>> GetChipColumns(int amount)
        {
            var columns = new List<List<int>>();
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