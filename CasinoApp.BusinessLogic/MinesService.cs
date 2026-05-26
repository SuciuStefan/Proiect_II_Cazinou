using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public enum MinesCellState { Hidden, Safe, Mine, MineBoom, SafeUnrevealed }
    public enum MinesGameState { Idle, Playing, GameOver, CashedOut }

    public record StartResult(
        double BalanceAfterBet,
        decimal BetAmount
    );

    public record RevealResult(
        bool HitMine,
        bool AutoCashOut,
        double NewBalance,
        decimal CashOutWin,
        string BetStatus
    );

    public record CashOutResult(
        decimal CashOutWin,
        double NewBalance
    );

    public interface IMinesService
    {
        MinesGameState GameState { get; }
        MinesCellState[] Cells { get; }
        int SafeRevealed { get; }
        double CurrentMultiplier { get; }
        decimal CurrentBet { get; }
        decimal LastBet { get; }
        decimal CashOutWin { get; }
        int GridSize { get; }
        int MineCount { get; }
        int Cols { get; }
        int MaxMines { get; }

        void SetGridSize(int gs);
        void SetMineCount(int mc);

        void AddChip(int denomination, double playerBalance, out string? error);
        void ClearBet();
        void HalfBet();
        void DoubleBet(double playerBalance, out string? error);
        void Rebet(double playerBalance);
        bool CanStart(double playerBalance);

        StartResult StartGame(double playerBalance);
        RevealResult RevealCell(int idx, double currentBalance);
        CashOutResult CashOut(double currentBalance);
        void NewRound();

        double GetMultiplier(int N, int M, int K);
        double GetNextMultiplier();
        double GetWinChance();

        string GetCellClass(MinesCellState s);
        string GetCellMult(int idx);
        string GetChanceColor(double p);
        string CellSizePx { get; }
    }

    public class MinesService : IMinesService
    {
        public static readonly int[] GridSizes = { 25, 36, 49, 64 };
        public static readonly int[] ChipDenoms = { 1, 5, 10, 25, 50, 100 };
        private const decimal MaxBet = 1_000_000m;

        public MinesGameState GameState { get; private set; } = MinesGameState.Idle;
        public MinesCellState[] Cells { get; private set; } = Array.Empty<MinesCellState>();
        public int SafeRevealed { get; private set; }
        public double CurrentMultiplier { get; private set; } = 1.0;
        public decimal CurrentBet { get; private set; }
        public decimal LastBet { get; private set; }
        public decimal CashOutWin { get; private set; }
        public int GridSize { get; private set; } = 25;
        public int MineCount { get; private set; } = 3;
        public int Cols => (int)Math.Sqrt(GridSize);
        public int MaxMines => GridSize - 2;

        private HashSet<int> _mineSet = new();
        private Dictionary<int, double> _cellMultipliers = new();

        public void SetGridSize(int gs)
        {
            GridSize = gs;
            MineCount = Math.Min(MineCount, MaxMines);
        }

        public void SetMineCount(int mc) =>
            MineCount = Math.Clamp(mc, 1, MaxMines);

        public void AddChip(int denomination, double playerBalance, out string? error)
        {
            error = null;
            decimal newBet = Math.Min(CurrentBet + denomination, MaxBet);
            if (playerBalance < (double)newBet)
            {
                error = "Balanta insuficienta!";
                return;
            }
            CurrentBet = newBet;
        }

        public void ClearBet() { CurrentBet = 0; }

        public void HalfBet() { CurrentBet = Math.Max(1, Math.Floor(CurrentBet / 2)); }

        public void DoubleBet(double playerBalance, out string? error)
        {
            error = null;
            decimal doubled = Math.Min(CurrentBet * 2, MaxBet);
            if (playerBalance < (double)doubled)
            {
                error = "Balanta insuficienta pentru dublare!";
                return;
            }
            CurrentBet = doubled;
        }

        public void Rebet(double playerBalance)
        {
            if (playerBalance >= (double)LastBet)
                CurrentBet = LastBet;
        }

        public bool CanStart(double playerBalance) =>
            CurrentBet > 0 && playerBalance >= (double)CurrentBet;

        public StartResult StartGame(double playerBalance)
        {
            double balAfterBet = playerBalance - (double)CurrentBet;

            var rng = new Random();
            _mineSet = new HashSet<int>();
            while (_mineSet.Count < MineCount)
                _mineSet.Add(rng.Next(GridSize));

            Cells = new MinesCellState[GridSize];
            _cellMultipliers = new();
            SafeRevealed = 0;
            CurrentMultiplier = 1.0;
            CashOutWin = 0;
            GameState = MinesGameState.Playing;

            return new StartResult(balAfterBet, CurrentBet);
        }

        public RevealResult RevealCell(int idx, double currentBalance)
        {
            if (GameState != MinesGameState.Playing)
                return new RevealResult(false, false, currentBalance, 0, "Pending");
            if (Cells[idx] != MinesCellState.Hidden)
                return new RevealResult(false, false, currentBalance, 0, "Pending");

            if (_mineSet.Contains(idx))
            {
                Cells[idx] = MinesCellState.MineBoom;
                RevealAllMines();
                RevealAllSafes();
                GameState = MinesGameState.GameOver;

                return new RevealResult(
                    HitMine: true,
                    AutoCashOut: false,
                    NewBalance: currentBalance,
                    CashOutWin: 0,
                    BetStatus: "Lost"
                );
            }
            else
            {
                SafeRevealed++;
                CurrentMultiplier = GetMultiplier(GridSize, MineCount, SafeRevealed);
                _cellMultipliers[idx] = CurrentMultiplier;
                Cells[idx] = MinesCellState.Safe;

                if (SafeRevealed == GridSize - MineCount)
                {
                    var co = CashOut(currentBalance);
                    return new RevealResult(
                        HitMine: false,
                        AutoCashOut: true,
                        NewBalance: co.NewBalance,
                        CashOutWin: co.CashOutWin,
                        BetStatus: "Won"
                    );
                }

                return new RevealResult(false, false, currentBalance, 0, "Pending");
            }
        }

        public CashOutResult CashOut(double currentBalance)
        {
            if (GameState != MinesGameState.Playing || SafeRevealed == 0)
                return new CashOutResult(0, currentBalance);

            CashOutWin = Math.Round(CurrentBet * (decimal)CurrentMultiplier, 2);
            double newBal = currentBalance + (double)CashOutWin;

            LastBet = CurrentBet;
            CurrentBet = 0;

            RevealAllMines();
            RevealAllSafes();
            GameState = MinesGameState.CashedOut;

            return new CashOutResult(CashOutWin, newBal);
        }

        public void NewRound()
        {
            Cells = Array.Empty<MinesCellState>();
            _mineSet = new();
            _cellMultipliers = new();
            SafeRevealed = 0;
            CurrentMultiplier = 1.0;
            CashOutWin = 0;
            GameState = MinesGameState.Idle;
        }

        public double GetMultiplier(int N, int M, int K)
        {
            if (K <= 0) return 1.0;
            double logRatio = 0;
            for (int i = 0; i < K; i++)
                logRatio += Math.Log(N - i) - Math.Log(N - M - i);
            return 0.99 * Math.Exp(logRatio);
        }

        public double GetNextMultiplier() =>
            GetMultiplier(GridSize, MineCount, SafeRevealed + 1);

        public double GetWinChance()
        {
            int remaining = GridSize - SafeRevealed;
            int safesLeft = remaining - MineCount;
            if (remaining <= 0) return 0;
            return 100.0 * safesLeft / remaining;
        }

        public string CellSizePx => GridSize switch
        {
            25 => "90px",
            36 => "76px",
            49 => "64px",
            _ => "54px"
        };

        public string GetCellClass(MinesCellState s) => s switch
        {
            MinesCellState.Safe => "cell-safe",
            MinesCellState.Mine => "cell-mine",
            MinesCellState.MineBoom => "cell-boom",
            MinesCellState.SafeUnrevealed => "cell-safe-ghost",
            _ => "cell-hidden"
        };

        public string GetCellMult(int idx) =>
            _cellMultipliers.TryGetValue(idx, out double m) ? m.ToString("F2") + "×" : "";

        public string GetChanceColor(double p) => p switch
        {
            >= 70 => "#2ecc71",
            >= 45 => "#f39c12",
            >= 20 => "#e67e22",
            _ => "#e74c3c"
        };

        private void RevealAllMines()
        {
            for (int i = 0; i < GridSize; i++)
                if (_mineSet.Contains(i) && Cells[i] == MinesCellState.Hidden)
                    Cells[i] = MinesCellState.Mine;
        }

        private void RevealAllSafes()
        {
            for (int i = 0; i < GridSize; i++)
                if (!_mineSet.Contains(i) && Cells[i] == MinesCellState.Hidden)
                    Cells[i] = MinesCellState.SafeUnrevealed;
        }
    }
}