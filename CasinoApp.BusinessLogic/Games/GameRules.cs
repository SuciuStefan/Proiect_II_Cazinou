namespace CasinoApp.BusinessLogic.Games;

public static class RouletteRules
{
    private static readonly HashSet<int> RedNumbers =
    [
        1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
    ];

    public static int GetPayoutMultiplier(string betKey, int winningNumber)
    {
        if (winningNumber < 0 || winningNumber > 36)
            return 0;

        if (TryGetNumberBet(betKey, out int betNumber))
            return betNumber == winningNumber ? winningNumber == 0 ? 14 : 36 : 0;

        return betKey switch
        {
            "red" when winningNumber != 0 && RedNumbers.Contains(winningNumber) => 2,
            "black" when winningNumber != 0 && !RedNumbers.Contains(winningNumber) => 2,
            "even" when winningNumber != 0 && winningNumber % 2 == 0 => 2,
            "odd" when winningNumber != 0 && winningNumber % 2 != 0 => 2,
            "low" when winningNumber is >= 1 and <= 18 => 2,
            "high" when winningNumber is >= 19 and <= 36 => 2,
            "doz1" when winningNumber is >= 1 and <= 12 => 3,
            "doz2" when winningNumber is >= 13 and <= 24 => 3,
            "doz3" when winningNumber is >= 25 and <= 36 => 3,
            "col1" when winningNumber != 0 && winningNumber % 3 == 1 => 3,
            "col2" when winningNumber != 0 && winningNumber % 3 == 2 => 3,
            "col3" when winningNumber != 0 && winningNumber % 3 == 0 => 3,
            _ => 0
        };
    }

    public static bool TryGetNumberBet(string betKey, out int number)
    {
        number = -1;
        return betKey.StartsWith("num-", StringComparison.Ordinal)
            && int.TryParse(betKey[4..], out number)
            && number >= 0
            && number <= 36;
    }
}

public enum BlackjackOutcome
{
    PlayerBust,
    DealerBlackjack,
    InsuredDealerBlackjack,
    PlayerBlackjack,
    DealerBust,
    PlayerWin,
    Push,
    DealerWin
}

public sealed record BlackjackSettlement(
    BlackjackOutcome Outcome,
    decimal NetWin,
    decimal BalanceAfterPayout
);

public static class BlackjackRules
{
    public static int GetHandValue(IEnumerable<(string Rank, int Value)> hand)
    {
        int total = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            if (card.Rank == "A")
            {
                aces++;
                total += 11;
            }
            else
            {
                total += card.Value;
            }
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    public static BlackjackSettlement SettleHand(
        int playerValue,
        int dealerValue,
        bool playerBlackjack,
        bool dealerBlackjack,
        decimal bet,
        decimal balance,
        bool insuranceTaken = false)
    {
        if (playerValue > 21)
            return new(BlackjackOutcome.PlayerBust, -bet, balance);

        if (dealerBlackjack && !playerBlackjack)
        {
            return insuranceTaken
                ? new(BlackjackOutcome.InsuredDealerBlackjack, 0, balance)
                : new(BlackjackOutcome.DealerBlackjack, -bet, balance);
        }

        if (playerBlackjack && !dealerBlackjack)
        {
            decimal win = Math.Round(bet * 1.5m, 2);
            return new(BlackjackOutcome.PlayerBlackjack, win, balance + bet + win);
        }

        if (dealerValue > 21)
            return new(BlackjackOutcome.DealerBust, bet, balance + bet * 2);

        if (playerValue > dealerValue)
            return new(BlackjackOutcome.PlayerWin, bet, balance + bet * 2);

        if (playerValue == dealerValue)
            return new(BlackjackOutcome.Push, 0, balance + bet);

        return new(BlackjackOutcome.DealerWin, -bet, balance);
    }
}

public static class BarbutRules
{
    public static int CountBeatenOpponents(int playerSum, IEnumerable<int> opponentSums) =>
        opponentSums.Count(sum => playerSum > sum);

    public static double GetReturn(double betAmount, int beatCount) => beatCount switch
    {
        3 => betAmount * 3,
        2 => betAmount * 2,
        1 => betAmount,
        _ => 0
    };
}

public static class MinesRules
{
    public static double GetMultiplier(int gridSize, int mineCount, int safeRevealed)
    {
        if (safeRevealed <= 0)
            return 1.0;

        double logRatio = 0;
        for (int i = 0; i < safeRevealed; i++)
            logRatio += Math.Log(gridSize - i) - Math.Log(gridSize - mineCount - i);

        return 0.99 * Math.Exp(logRatio);
    }

    public static double GetWinChance(int gridSize, int mineCount, int safeRevealed)
    {
        int remaining = gridSize - safeRevealed;
        if (remaining <= 0)
            return 0;

        int safesLeft = remaining - mineCount;
        return 100.0 * safesLeft / remaining;
    }
}

public static class SlotsRules
{
    public static int CountLeftAlignedMatches(string[,] grid, IReadOnlyList<int> payline)
    {
        string first = grid[0, payline[0]];
        int matchCount = 1;

        for (int reel = 1; reel < payline.Count; reel++)
        {
            if (grid[reel, payline[reel]] != first)
                break;

            matchCount++;
        }

        return matchCount;
    }

    public static double GetWinMultiplier(int matchCount, double mult3, double mult4, double mult5) =>
        matchCount switch
        {
            >= 5 => mult5,
            4 => mult4,
            3 => mult3,
            _ => 0
        };
}

public static class ScratchCardRules
{
    public static IReadOnlyList<int> GetWinningRows(IReadOnlyList<string> grid)
    {
        var rows = new List<int>();
        for (int row = 0; row < 3; row++)
        {
            string first = grid[row * 3];
            if (first == grid[row * 3 + 1] && first == grid[row * 3 + 2])
                rows.Add(row);
        }

        return rows;
    }
}

public static class FlipCoinRules
{
    public const decimal TableLimit = 10_000m;

    public static decimal GetWinningPot(decimal currentBet) => currentBet * 2;

    public static bool ReachedTableLimit(decimal pot) => pot >= TableLimit;
}

public static class WheelOfFortuneRules
{
    public static double GetLandingAngle(int winningSlotIndex, int slotCount) =>
        360.0 - (winningSlotIndex + 0.5) * (360.0 / slotCount);
}
