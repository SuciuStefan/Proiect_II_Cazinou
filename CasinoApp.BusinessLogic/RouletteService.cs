using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public interface IRouletteService
    {
        int[] WheelOrder { get; }

        IReadOnlyDictionary<string, int> Bets { get; }
        int TotalBet { get; }
        void PlaceBet(string betKey, int chip, double playerBalance, out string? error);
        void ClearBets();

        SpinSetup PrepareSpin(double playerBalance, out string? error);
        SpinResult ResolveSpin(int winningNumber, double balanceAfterBet);

        bool IsRed(int n);
        string GetChipColorClass(int totalBetOnCell);
        string GetLastResultColorClass(int n);
        string GetLastResultProperties(int n);
        string GetHistoryColorClass(int n);
        string GetWheelSegmentColor(int n);
    }

    public record SpinSetup(
        int WinningIndex,
        int WinningNumber,
        double NewWheelDeg,
        double NewBallDeg,
        double BalanceAfterBet,
        int TotalBet
    );

    public record SpinResult(
        double TotalWin,
        string BetStatus
    );

    public class RouletteService : IRouletteService
    {
        public int[] WheelOrder { get; } =
        {
            0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26
        };

        private static readonly HashSet<int> RedNumbers = new()
        {
            1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36
        };

        private Dictionary<string, int> _bets = new();
        public IReadOnlyDictionary<string, int> Bets => _bets;
        public int TotalBet => _bets.Values.Sum();

        private double _currentWheelDeg = 0;
        private double _currentBallDeg = 0;

        private readonly Random _rng = new();

        public void PlaceBet(string betKey, int chip, double playerBalance, out string? error)
        {
            error = null;
            if (playerBalance < TotalBet + chip)
            {
                error = "Balanță insuficientă!";
                return;
            }
            if (_bets.ContainsKey(betKey)) _bets[betKey] += chip;
            else _bets[betKey] = chip;
        }

        public void ClearBets() => _bets = new();

        public SpinSetup PrepareSpin(double playerBalance, out string? error)
        {
            error = null;
            int total = TotalBet;

            if (total == 0) { error = "Plasează un pariu mai întâi!"; return null!; }
            if (playerBalance < total) { error = "Balanță insuficientă!"; return null!; }

            int resIdx = _rng.Next(WheelOrder.Length);
            int winningNumber = WheelOrder[resIdx];

            double segDeg = 360.0 / WheelOrder.Length;
            double extraSpins = (6 + _rng.Next(0, 4)) * 360.0;
            double currentNorm = _currentWheelDeg % 360;
            double winningAngle = (WheelOrder.Length - resIdx - 0.5) * segDeg;
            double adjustment = (winningAngle - currentNorm + 360) % 360;
            _currentWheelDeg += extraSpins + adjustment;

            double ballExtra = (8 + _rng.Next(0, 5)) * 360.0;
            _currentBallDeg -= ballExtra;

            double balAfterBet = playerBalance - total;

            return new SpinSetup(
                WinningIndex: resIdx,
                WinningNumber: winningNumber,
                NewWheelDeg: _currentWheelDeg,
                NewBallDeg: _currentBallDeg,
                BalanceAfterBet: balAfterBet,
                TotalBet: total
            );
        }

        public SpinResult ResolveSpin(int winningNumber, double balanceAfterBet)
        {
            double totalWin = 0;
            int n = winningNumber;

            foreach (var (key, amount) in _bets)
            {
                double payout = 0;

                if (TryGetNumberBet(key, out int betNumber))
                {
                    if (betNumber == n) payout = amount * (n == 0 ? 14 : 36);
                }
                else payout = key switch
                {
                    "red" when IsRed(n) && n != 0 => amount * 2,
                    "black" when !IsRed(n) && n != 0 => amount * 2,
                    "even" when n != 0 && n % 2 == 0 => amount * 2,
                    "odd" when n != 0 && n % 2 != 0 => amount * 2,
                    "low" when n >= 1 && n <= 18 => amount * 2,
                    "high" when n >= 19 && n <= 36 => amount * 2,
                    "doz1" when n >= 1 && n <= 12 => amount * 3,
                    "doz2" when n >= 13 && n <= 24 => amount * 3,
                    "doz3" when n >= 25 && n <= 36 => amount * 3,
                    "col1" when n != 0 && n % 3 == 1 => amount * 3,
                    "col2" when n != 0 && n % 3 == 2 => amount * 3,
                    "col3" when n != 0 && n % 3 == 0 => amount * 3,
                    _ => 0
                };

                totalWin += payout;
            }

            _bets = new();

            return new SpinResult(
                TotalWin: totalWin,
                BetStatus: totalWin > 0 ? "Won" : "Lost"
            );
        }

        public bool IsRed(int n) => RedNumbers.Contains(n);

        public string GetChipColorClass(int totalBetOnCell)
        {
            if (totalBetOnCell >= 100) return "chip-100";
            if (totalBetOnCell >= 50) return "chip-50";
            if (totalBetOnCell >= 25) return "chip-25";
            if (totalBetOnCell >= 10) return "chip-10";
            if (totalBetOnCell >= 5) return "chip-5";
            return "chip-1";
        }

        public string GetLastResultColorClass(int n) =>
            n == 0 ? "lr-green" : IsRed(n) ? "lr-red" : "lr-black";

        public string GetLastResultProperties(int n)
        {
            if (n == 0) return "ZERO";
            string color = IsRed(n) ? "ROȘU" : "NEGRU";
            string parity = n % 2 == 0 ? "PAR" : "IMPAR";
            string range = n <= 18 ? "1-18" : "19-36";
            return $"{color} · {parity} · {range}";
        }

        public string GetHistoryColorClass(int n) =>
            n == 0 ? "hn-green" : IsRed(n) ? "hn-red" : "hn-black";

        public string GetWheelSegmentColor(int n) =>
            n == 0 ? "#1a6b1a" : IsRed(n) ? "#8B0000" : "#0f0f0f";

        private static bool TryGetNumberBet(string key, out int number)
        {
            number = -1;
            return key.StartsWith("num-")
                && int.TryParse(key[4..], out number)
                && number >= 0
                && number <= 36;
        }
    }
}