using CasinoApp.BusinessLogic.Games;

namespace CasinoApp.Tests;

public class RouletteRulesTests
{
    [Fact]
    public void ExactNumberBetPaysThirtySixTimes()
    {
        Assert.Equal(36, RouletteRules.GetPayoutMultiplier("num-22", 22));
        Assert.Equal(0, RouletteRules.GetPayoutMultiplier("num-22", 24));
    }

    [Fact]
    public void ZeroBetUsesProjectZeroMultiplier()
    {
        Assert.Equal(14, RouletteRules.GetPayoutMultiplier("num-0", 0));
        Assert.Equal(0, RouletteRules.GetPayoutMultiplier("even", 0));
    }

    [Theory]
    [InlineData("red", 22, 0)]
    [InlineData("black", 22, 2)]
    [InlineData("even", 22, 2)]
    [InlineData("high", 22, 2)]
    [InlineData("doz2", 22, 3)]
    [InlineData("col1", 22, 3)]
    public void OutsideBetsUseExpectedMultiplier(string betKey, int result, int expectedMultiplier)
    {
        Assert.Equal(expectedMultiplier, RouletteRules.GetPayoutMultiplier(betKey, result));
    }
}

public class BlackjackRulesTests
{
    [Fact]
    public void HandValueDowngradesAcesToAvoidBust()
    {
        var hand = new[] { ("A", 1), ("A", 1), ("9", 9) };

        Assert.Equal(21, BlackjackRules.GetHandValue(hand));
    }

    [Fact]
    public void BlackjackPaysThreeToTwoAndReturnsBet()
    {
        var result = BlackjackRules.SettleHand(21, 19, true, false, 10m, 90m);

        Assert.Equal(BlackjackOutcome.PlayerBlackjack, result.Outcome);
        Assert.Equal(15m, result.NetWin);
        Assert.Equal(115m, result.BalanceAfterPayout);
    }

    [Fact]
    public void PushReturnsOriginalBet()
    {
        var result = BlackjackRules.SettleHand(18, 18, false, false, 25m, 75m);

        Assert.Equal(BlackjackOutcome.Push, result.Outcome);
        Assert.Equal(0m, result.NetWin);
        Assert.Equal(100m, result.BalanceAfterPayout);
    }
}

public class BarbutRulesTests
{
    [Fact]
    public void BeatingTwoOpponentsReturnsDoubleBet()
    {
        int beatCount = BarbutRules.CountBeatenOpponents(11, [8, 11, 9]);

        Assert.Equal(2, beatCount);
        Assert.Equal(100, BarbutRules.GetReturn(50, beatCount));
    }
}

public class MinesRulesTests
{
    [Fact]
    public void FirstSafeRevealUsesConfiguredHouseEdge()
    {
        double multiplier = MinesRules.GetMultiplier(25, 3, 1);

        Assert.Equal(1.125, multiplier, 3);
    }

    [Fact]
    public void StartingWinChanceCountsSafeCells()
    {
        Assert.Equal(88, MinesRules.GetWinChance(25, 3, 0), 3);
    }
}

public class SlotsRulesTests
{
    [Fact]
    public void MatchCounterStopsAtFirstDifferentReel()
    {
        var grid = new string[5, 3];
        int[] payline = [1, 1, 1, 1, 1];
        grid[0, 1] = "A";
        grid[1, 1] = "A";
        grid[2, 1] = "A";
        grid[3, 1] = "B";
        grid[4, 1] = "A";

        Assert.Equal(3, SlotsRules.CountLeftAlignedMatches(grid, payline));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 5)]
    [InlineData(4, 10)]
    [InlineData(5, 25)]
    public void MultiplierUsesMatchedSymbolCount(int matches, double expected)
    {
        Assert.Equal(expected, SlotsRules.GetWinMultiplier(matches, 5, 10, 25));
    }
}

public class ScratchCardRulesTests
{
    [Fact]
    public void MatchingRowsAreReported()
    {
        string[] grid =
        [
            "Cherry", "Cherry", "Cherry",
            "Bell", "Star", "Bell",
            "Seven", "Seven", "Seven"
        ];

        Assert.Equal([0, 2], ScratchCardRules.GetWinningRows(grid));
    }
}

public class FlipCoinRulesTests
{
    [Fact]
    public void WinDoublesPotAndTableLimitCashoutThresholdIsInclusive()
    {
        decimal pot = FlipCoinRules.GetWinningPot(5_000m);

        Assert.Equal(10_000m, pot);
        Assert.True(FlipCoinRules.ReachedTableLimit(pot));
    }
}

public class WheelOfFortuneRulesTests
{
    [Fact]
    public void LandingAngleTargetsCenterOfWinningSlot()
    {
        Assert.Equal(337.5, WheelOfFortuneRules.GetLandingAngle(0, 8), 3);
        Assert.Equal(202.5, WheelOfFortuneRules.GetLandingAngle(3, 8), 3);
    }
}
