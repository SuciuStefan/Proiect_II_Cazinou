using System;
using System.Collections.Generic;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public interface IBlackjackService
    {
        IReadOnlyList<Card> PlayerHand { get; }
        IReadOnlyList<Card> DealerHand { get; }
        IReadOnlyList<Card> SplitHand { get; }
        int DeckCount { get; }
        bool HasSplit { get; }
        bool IsPlayingSplitHand { get; }

        void PlaceChip(int denomination, double playerBalance, out string? error);
        void ClearBet();
        void Rebet(double playerBalance);
        decimal CurrentBet { get; }
        decimal LastBet { get; }
        decimal OriginalBet { get; }

        bool IsOfferingInsurance { get; }
        decimal InsuranceBet { get; }

        bool CanDouble(double playerBalance);
        bool CanSplit(double playerBalance);

        DealResult StartDeal(double playerBalance, int blackjackGameId);
        InsuranceResult TakeInsurance(double playerBalance);
        InsuranceResult DeclineInsurance();
        HitResult Hit();
        StandResult Stand();
        DoubleResult DoubleDown(double playerBalance);
        SplitResult DoSplit(double playerBalance);

        EndResult EndGame();

        Card? DealerDrawNext();
        bool AllPlayerHandsBust { get; }

        void ResetRound();
        void ResetBet();

        int GetHandValue(IReadOnlyList<Card> hand);
        string GetColorClass(Card card);
        List<List<int>> GetChipColumns(int amount);
        string GetResultClass(string msg);
    }

    public record Card(string Rank, string Suit, int Value);

    public record DealResult(
        bool NeedsInsurancePrompt,
        bool DealerPeekedBJ,
        bool PlayerNaturalBJ,
        double BalanceAfterBet,
        decimal BetAmount
    );

    public record InsuranceResult(
        bool DealerHasBJ,
        bool PlayerHasBJ,
        decimal InsuranceAmount,
        double BalanceAfterInsurance
    );

    public record HitResult(
        bool PlayerBust,
        bool ShouldSwitchToSplitHand,
        bool ShouldEndGame
    );

    public record StandResult(
        bool ShouldSwitchToSplitHand,
        bool ShouldDealerPlay
    );

    public record DoubleResult(
        double BalanceAfterDouble,
        decimal NewBetAmount,
        bool PlayerBust,
        bool ShouldSwitchToSplitHand,
        bool ShouldDealerPlay
    );

    public record SplitResult(
        double BalanceAfterSplit,
        decimal SplitBetAmount,
        bool IsAceSplit
    );

    public record EndResult(
        string MainResultMsg,
        decimal MainResultWin,
        string SplitResultMsg,
        decimal SplitResultWin,
        double NewBalance,
        string BetStatus
    );

    public class BlackjackService : IBlackjackService
    {
        private List<Card> _deck = new();
        private List<Card> _playerHand = new();
        private List<Card> _dealerHand = new();
        private List<Card> _splitHand = new();

        private decimal _currentBet = 0;
        private decimal _lastBet = 0;
        private decimal _originalBet = 0;
        private decimal _splitBet = 0;
        private decimal _insuranceBet = 0;

        private bool _hasSplit = false;
        private bool _isPlayingSplitHand = false;
        private bool _mainHandDone = false;
        private bool _isOfferingInsurance = false;

        private readonly int[] _chipDenoms = { 1, 5, 10, 25, 50, 100 };

        public IReadOnlyList<Card> PlayerHand => _playerHand;
        public IReadOnlyList<Card> DealerHand => _dealerHand;
        public IReadOnlyList<Card> SplitHand => _splitHand;
        public int DeckCount => _deck.Count;
        public bool HasSplit => _hasSplit;
        public bool IsPlayingSplitHand => _isPlayingSplitHand;
        public decimal CurrentBet => _currentBet;
        public decimal LastBet => _lastBet;
        public decimal OriginalBet => _originalBet;
        public bool IsOfferingInsurance => _isOfferingInsurance;
        public decimal InsuranceBet => _insuranceBet;

        public bool AllPlayerHandsBust =>
            GetHandValue(_playerHand) > 21 &&
            (!_hasSplit || GetHandValue(_splitHand) > 21);

        public void PlaceChip(int denomination, double playerBalance, out string? error)
        {
            error = null;
            if (playerBalance < (double)(_currentBet + denomination))
            {
                error = "Balanta insuficienta!";
                return;
            }
            _currentBet += denomination;
        }

        public void ClearBet() => _currentBet = 0;

        public void Rebet(double playerBalance)
        {
            if (_lastBet > 0 && playerBalance >= (double)_lastBet)
                _currentBet = _lastBet;
        }

        public bool CanDouble(double playerBalance) =>
            !_isPlayingSplitHand &&
            _playerHand.Count == 2 &&
            playerBalance >= (double)_currentBet;

        public bool CanSplit(double playerBalance) =>
            !_hasSplit &&
            _playerHand.Count == 2 &&
            _playerHand[0].Rank == _playerHand[1].Rank &&
            playerBalance >= (double)_currentBet;

        public DealResult StartDeal(double playerBalance, int blackjackGameId)
        {
            _hasSplit = false;
            _isPlayingSplitHand = false;
            _mainHandDone = false;
            _splitHand = new();
            _splitBet = 0;
            _insuranceBet = 0;
            _isOfferingInsurance = false;

            _originalBet = _currentBet;
            double balAfterBet = playerBalance - (double)_currentBet;

            if (_deck.Count < 78) BuildDeck();

            _playerHand = new();
            _dealerHand = new();

            _playerHand.Add(Draw()); _dealerHand.Add(Draw());
            _playerHand.Add(Draw()); _dealerHand.Add(Draw());

            if (_dealerHand[0].Rank == "A")
            {
                _isOfferingInsurance = true;
                return new DealResult(
                    NeedsInsurancePrompt: true,
                    DealerPeekedBJ: false,
                    PlayerNaturalBJ: false,
                    BalanceAfterBet: balAfterBet,
                    BetAmount: _currentBet
                );
            }

            if (_dealerHand[0].Value == 10 && GetHandValue(_dealerHand) == 21)
            {
                return new DealResult(
                    NeedsInsurancePrompt: false,
                    DealerPeekedBJ: true,
                    PlayerNaturalBJ: false,
                    BalanceAfterBet: balAfterBet,
                    BetAmount: _currentBet
                );
            }

            bool playerBJ = GetHandValue(_playerHand) == 21;

            return new DealResult(
                NeedsInsurancePrompt: false,
                DealerPeekedBJ: false,
                PlayerNaturalBJ: playerBJ,
                BalanceAfterBet: balAfterBet,
                BetAmount: _currentBet
            );
        }

        public InsuranceResult TakeInsurance(double playerBalance)
        {
            decimal insuranceAmount = Math.Floor(_currentBet / 2);
            _isOfferingInsurance = false;

            if (playerBalance < (double)insuranceAmount)
                return DeclineInsurance();

            _insuranceBet = insuranceAmount;
            double balAfter = playerBalance - (double)insuranceAmount;

            bool dealerBJ = GetHandValue(_dealerHand) == 21;
            bool playerBJ = GetHandValue(_playerHand) == 21 && _playerHand.Count == 2;

            return new InsuranceResult(
                DealerHasBJ: dealerBJ,
                PlayerHasBJ: playerBJ,
                InsuranceAmount: insuranceAmount,
                BalanceAfterInsurance: balAfter
            );
        }

        public InsuranceResult DeclineInsurance()
        {
            _insuranceBet = 0;
            _isOfferingInsurance = false;

            bool dealerBJ = GetHandValue(_dealerHand) == 21;
            bool playerBJ = GetHandValue(_playerHand) == 21 && _playerHand.Count == 2;

            return new InsuranceResult(
                DealerHasBJ: dealerBJ,
                PlayerHasBJ: playerBJ,
                InsuranceAmount: 0,
                BalanceAfterInsurance: 0
            );
        }

        public HitResult Hit()
        {
            if (_isPlayingSplitHand)
            {
                _splitHand.Add(Draw());
                if (GetHandValue(_splitHand) >= 21)
                    return new HitResult(PlayerBust: false, ShouldSwitchToSplitHand: false, ShouldEndGame: true);
            }
            else
            {
                _playerHand.Add(Draw());
                int pv = GetHandValue(_playerHand);
                if (pv >= 21)
                {
                    if (_hasSplit && !_mainHandDone)
                    {
                        _mainHandDone = true;
                        _isPlayingSplitHand = true;
                        return new HitResult(PlayerBust: false, ShouldSwitchToSplitHand: true, ShouldEndGame: false);
                    }
                    return new HitResult(PlayerBust: pv > 21, ShouldSwitchToSplitHand: false, ShouldEndGame: true);
                }
            }
            return new HitResult(PlayerBust: false, ShouldSwitchToSplitHand: false, ShouldEndGame: false);
        }

        public StandResult Stand()
        {
            if (_hasSplit && !_isPlayingSplitHand && !_mainHandDone)
            {
                _mainHandDone = true;
                _isPlayingSplitHand = true;
                return new StandResult(ShouldSwitchToSplitHand: true, ShouldDealerPlay: false);
            }
            return new StandResult(ShouldSwitchToSplitHand: false, ShouldDealerPlay: true);
        }

        public DoubleResult DoubleDown(double playerBalance)
        {
            double balAfter = playerBalance - (double)_currentBet;
            _currentBet *= 2;

            _playerHand.Add(Draw());
            int pv = GetHandValue(_playerHand);

            bool switchToSplit = _hasSplit && !_mainHandDone;
            bool playerBust = !switchToSplit && pv > 21;
            bool shouldDealerPlay = !switchToSplit && !playerBust;

            if (switchToSplit) { _mainHandDone = true; _isPlayingSplitHand = true; }

            return new DoubleResult(
                BalanceAfterDouble: balAfter,
                NewBetAmount: _currentBet,
                PlayerBust: playerBust,
                ShouldSwitchToSplitHand: switchToSplit,
                ShouldDealerPlay: shouldDealerPlay
            );
        }

        public SplitResult DoSplit(double playerBalance)
        {
            double balAfter = playerBalance - (double)_currentBet;
            _splitBet = _currentBet;
            _hasSplit = true;
            _isPlayingSplitHand = false;
            _mainHandDone = false;

            var secondCard = _playerHand[1];
            _playerHand.RemoveAt(1);
            _splitHand = new List<Card> { secondCard };

            _playerHand.Add(Draw());
            _splitHand.Add(Draw());

            bool isAceSplit = _playerHand[0].Rank == "A";
            if (isAceSplit)
            {
                _mainHandDone = true;
                _isPlayingSplitHand = true;
            }

            return new SplitResult(
                BalanceAfterSplit: balAfter,
                SplitBetAmount: _splitBet,
                IsAceSplit: isAceSplit
            );
        }

        public Card? DealerDrawNext()
        {
            if (AllPlayerHandsBust) return null;
            if (GetHandValue(_dealerHand) >= 17) return null;

            var card = Draw();
            _dealerHand.Add(card);
            return card;
        }

        public EndResult EndGame()
        {
            int pv = GetHandValue(_playerHand);
            int dv = GetHandValue(_dealerHand);
            bool playerBJ = pv == 21 && _playerHand.Count == 2 && !_hasSplit;
            bool dealerBJ = dv == 21 && _dealerHand.Count == 2;

            decimal currentBalance = 0;
            decimal newBalance = currentBalance;

            if (_insuranceBet > 0 && dealerBJ)
                newBalance += _insuranceBet * 3;

            var (mainMsg, mainWin, nb1) = ComputeHandResult(
                pv, dv, playerBJ, dealerBJ, _currentBet, newBalance, _insuranceBet);
            newBalance = nb1;

            string splitMsg = "";
            decimal splitWin = 0;
            if (_hasSplit)
            {
                int spv = GetHandValue(_splitHand);
                var (sm, sw, nb2) = ComputeHandResult(spv, dv, false, dealerBJ, _splitBet, newBalance);
                splitMsg = sm;
                splitWin = sw;
                newBalance = nb2;
            }

            decimal totalResult = mainWin + splitWin;
            string betStatus = totalResult > 0 ? "Won" : totalResult == 0 ? "Push" : "Lost";

            _lastBet = _originalBet;
            _currentBet = 0;

            return new EndResult(
                MainResultMsg: mainMsg,
                MainResultWin: mainWin,
                SplitResultMsg: splitMsg,
                SplitResultWin: splitWin,
                NewBalance: (double)newBalance,
                BetStatus: betStatus
            );
        }

        public void ResetRound()
        {
            _playerHand = new();
            _dealerHand = new();
            _splitHand = new();
            _hasSplit = false;
            _isPlayingSplitHand = false;
            _mainHandDone = false;
            _insuranceBet = 0;
            _isOfferingInsurance = false;
        }

        public void ResetBet() => _currentBet = 0;

        public int GetHandValue(IReadOnlyList<Card> hand)
        {
            int total = 0, aces = 0;
            foreach (var c in hand)
            {
                if (c.Rank == "A") { aces++; total += 11; }
                else total += c.Value;
            }
            while (total > 21 && aces > 0) { total -= 10; aces--; }
            return total;
        }

        public string GetColorClass(Card c) =>
            c.Suit is "♥" or "♦" ? "card-red" : "card-black";

        public string GetResultClass(string msg) => msg switch
        {
            var m when m.Contains("BLACKJACK") && m.Contains("✦") => "res-bj",
            var m when m.Contains("ASIGURAREA") => "res-push",
            var m when m.Contains("CASTIGAT") => "res-win",
            var m when m.Contains("EGALITATE") => "res-push",
            _ => "res-lose"
        };

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

        private void BuildDeck()
        {
            _deck = new();
            var suits = new[] { "♠", "♥", "♦", "♣" };
            var ranks = new (string R, int V)[]
            {
                ("A",1),  ("2",2), ("3",3), ("4",4),  ("5",5),
                ("6",6),  ("7",7), ("8",8), ("9",9),  ("10",10),
                ("J",10), ("Q",10),("K",10)
            };
            for (int d = 0; d < 6; d++)
                foreach (var s in suits)
                    foreach (var r in ranks)
                        _deck.Add(new Card(r.R, s, r.V));

            var rng = new Random();
            _deck = _deck.OrderBy(_ => rng.Next()).ToList();
        }

        private Card Draw()
        {
            var c = _deck[^1];
            _deck.RemoveAt(_deck.Count - 1);
            return c;
        }

        private static (string msg, decimal win, decimal newBalance) ComputeHandResult(
            int pv, int dv, bool playerBJ, bool dealerBJ,
            decimal bet, decimal balance, decimal insuranceBet = 0)
        {
            string msg;
            decimal win;
            decimal nb = balance;

            if (pv > 21)
            {
                msg = "BUST — AI PIERDUT";
                win = -bet;
            }
            else if (dealerBJ && !playerBJ)
            {
                if (insuranceBet > 0)
                {
                    msg = "ASIGURAREA TE-A SALVAT!";
                    win = 0;
                }
                else
                {
                    msg = "DEALER BLACKJACK";
                    win = -bet;
                }
            }
            else if (playerBJ && !dealerBJ)
            {
                decimal winAmt = Math.Round(bet * 1.5m, 2);
                msg = "✦ BLACKJACK! ✦";
                win = winAmt;
                nb += bet + winAmt;
            }
            else if (dv > 21)
            {
                msg = "DEALER BUST — AI CASTIGAT!";
                win = bet;
                nb += bet * 2;
            }
            else if (pv > dv)
            {
                msg = "AI CASTIGAT!";
                win = bet;
                nb += bet * 2;
            }
            else if (pv == dv)
            {
                msg = "EGALITATE — PUSH";
                win = 0;
                nb += bet;
            }
            else
            {
                msg = "DEALER CASTIGA";
                win = -bet;
            }

            return (msg, win, nb);
        }
    }
}