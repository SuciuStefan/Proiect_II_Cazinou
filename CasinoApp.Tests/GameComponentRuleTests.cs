using CasinoApp.BusinessLogic.Services;
using CasinoApp.DataAccess.DB_operations;
using CasinoApp.DataAccess.Entities;
using CasinoApp.Web.Components.Pages;
using CasinoApp.Web.Components.Pages.Games;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CasinoApp.Tests;

public class BarbutServiceTests
{
    [Fact]
    public void PickRandomAINames_PopulatesNamesAndAvatars()
    {
        var service = new BarbutService();

        service.PickRandomAINames();

        Assert.Equal(3, service.AINames.Length);
        Assert.Equal(3, service.AIAvatars.Length);
        Assert.All(service.AINames, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.All(service.AIAvatars, avatar => Assert.False(string.IsNullOrWhiteSpace(avatar)));
    }

    [Fact]
    public void PrepareRoll_CalculatesCorrectInitialState()
    {
        var service = new BarbutService();
        double initialBalance = 1000;
        int betAmount = 50;
        int diceCount = 2;

        var setup = service.PrepareRoll(betAmount, diceCount, initialBalance);

        Assert.Equal(950, setup.BalanceAfterBet);
        Assert.Equal(2, setup.PlayerDice.Length);
        Assert.Equal(3, setup.AIDice.Length);
        Assert.Equal(3, setup.AISums.Length);

        Assert.All(setup.PlayerDice, die => Assert.InRange(die, 1, 6));
    }

    [Fact]
    public void ResolveRoll_UpdatesBalanceCorrectly_BasedOnNetGain()
    {
        var service = new BarbutService();
        double balanceAfterBet = 950;
        int betAmount = 50;

        var setup = service.PrepareRoll(betAmount, 2, 1000);
        var result = service.ResolveRoll(betAmount, balanceAfterBet);

        double expectedReturns = setup.NetGain + betAmount;
        bool expectedWin = expectedReturns > 0;
        double expectedBalance = expectedWin ? balanceAfterBet + expectedReturns : balanceAfterBet;

        Assert.Equal(expectedBalance, result.NewBalance);
        Assert.Equal(setup.NetGain > 0 ? "Won" : setup.NetGain == 0 ? "Push" : "Lost", result.BetStatus);
    }

    [Theory]
    [InlineData(3, "card-winner", "sum-win")]
    [InlineData(0, "card-loser", "sum-lose")]
    [InlineData(1, "card-neutral", "sum-push")]
    public void UIHelpers_ReturnCorrectCSSClasses_ForPlayer(int forceBeatCount, string expectedCard, string expectedSum)
    {
        var service = new BarbutService();

        typeof(BarbutService).GetProperty("BeatCount")?.SetValue(service, forceBeatCount);

        var cardClass = service.GetPlayerCardClass();
        var sumClass = service.GetPlayerSumClass();

        Assert.Equal(expectedCard, cardClass);
        Assert.Equal(expectedSum, sumClass);
    }
}

public class BlackjackServiceTests
{
    [Fact]
    public void PlaceChip_InsufficientBalance_SetsErrorAndDoesNotUpdateBet()
    {
        var service = new BlackjackService();

        service.PlaceChip(25, 10, out string? errorMsg);

        Assert.Equal("Balanta insuficienta!", errorMsg);
        Assert.Equal(0m, service.CurrentBet);
    }

    [Fact]
    public void PlaceChip_SufficientBalance_UpdatesBetAndClearsError()
    {
        var service = new BlackjackService();

        service.PlaceChip(25, 100, out string? errorMsg);

        Assert.Null(errorMsg);
        Assert.Equal(25m, service.CurrentBet);
    }

    [Fact]
    public void ClearBet_ResetsBetAmount()
    {
        var service = new BlackjackService();
        service.PlaceChip(50, 100, out _);

        service.ClearBet();

        Assert.Equal(0m, service.CurrentBet);
    }

    [Fact]
    public void Rebet_SufficientBalance_UpdatesBetToLastBet()
    {
        var service = new BlackjackService();

        typeof(BlackjackService)
            .GetField("_lastBet", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(service, 25m);

        service.Rebet(100);

        Assert.Equal(25m, service.CurrentBet);
    }

    [Theory]
    [InlineData(22, 10, false, false, 10.0, 100.0, 0.0, "BUST — AI PIERDUT", -10.0, 100.0)]
    [InlineData(20, 21, false, true, 10.0, 100.0, 0.0, "DEALER BLACKJACK", -10.0, 100.0)]
    [InlineData(20, 21, false, true, 10.0, 100.0, 5.0, "ASIGURAREA TE-A SALVAT!", 0.0, 100.0)]
    [InlineData(21, 10, true, false, 10.0, 100.0, 0.0, "✦ BLACKJACK! ✦", 15.0, 125.0)]
    [InlineData(20, 22, false, false, 10.0, 100.0, 0.0, "DEALER BUST — AI CASTIGAT!", 10.0, 120.0)]
    [InlineData(20, 18, false, false, 10.0, 100.0, 0.0, "AI CASTIGAT!", 10.0, 120.0)]
    [InlineData(20, 20, false, false, 10.0, 100.0, 0.0, "EGALITATE — PUSH", 0.0, 110.0)]
    [InlineData(17, 20, false, false, 10.0, 100.0, 0.0, "DEALER CASTIGA", -10.0, 100.0)]
    public void ComputeHandResult_EvaluatesAllGameConditionsCorrectly(
      int pv, int dv, bool playerBJ, bool dealerBJ,
      double bet, double balance, double insuranceBet,
      string expectedMsg, double expectedWin, double expectedNewBalance)
    {
        var method = typeof(BlackjackService).GetMethod("ComputeHandResult", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        decimal mBet = (decimal)bet;
        decimal mBalance = (decimal)balance;
        decimal mInsuranceBet = (decimal)insuranceBet;

        var result = method.Invoke(null, new object[] { pv, dv, playerBJ, dealerBJ, mBet, mBalance, mInsuranceBet });
        var (msg, win, newBalance) = ((string, decimal, decimal))result!;

        Assert.Equal(expectedMsg, msg);
        Assert.Equal((decimal)expectedWin, win);
        Assert.Equal((decimal)expectedNewBalance, newBalance);
    }

    [Fact]
    public void GetHandValue_CalculatesAcesProperly()
    {
        var service = new BlackjackService();

        var hand1 = new List<Card> { new Card("A", "♠", 1), new Card("K", "♥", 10) };
        Assert.Equal(21, service.GetHandValue(hand1));

        var hand2 = new List<Card> { new Card("A", "♠", 1), new Card("A", "♥", 1), new Card("K", "♦", 10) };
        Assert.Equal(12, service.GetHandValue(hand2));
    }
}

public class FlipACoinServiceTests
{
    [Fact]
    public void AddChip_InsufficientBalance_SetsErrorAndDoesNotUpdateBet()
    {
        var service = new FlipACoinService();

        service.AddChip(25, 10, out string? errorMsg);

        Assert.Equal("Balanta insuficienta!", errorMsg);
        Assert.Equal(0m, service.CurrentBet);
    }

    [Fact]
    public void AddChip_DuringStreak_SetsStreakErrorMsg()
    {
        var service = new FlipACoinService();

        typeof(FlipACoinService).GetProperty("IsStreak")?.SetValue(service, true);
        typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 50m);

        service.AddChip(10, 1000, out string? errorMsg);

        Assert.Equal("Ești pe dublaj! Dă FLIP sau apasă pe încasare.", errorMsg);
        Assert.Equal(50m, service.CurrentBet);
    }

    [Fact]
    public void ClearBet_ResetsBetAmountAndStreak()
    {
        var service = new FlipACoinService();
        typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 50m);
        typeof(FlipACoinService).GetProperty("IsStreak")?.SetValue(service, true);

        service.ClearBet();

        Assert.Equal(0m, service.CurrentBet);
        Assert.False(service.IsStreak);
    }

    [Fact]
    public void CollectStreak_AddsBetToBalanceAndClearsBet()
    {
        var service = new FlipACoinService();
        typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 150m);
        typeof(FlipACoinService).GetProperty("IsStreak")?.SetValue(service, true);

        var result = service.CollectStreak(100.0);

        Assert.Equal(250.0, result.NewBalance);
        Assert.Equal(0m, service.CurrentBet);
        Assert.False(service.IsStreak);
        Assert.Equal("Won", result.BetStatus);
    }

    [Fact]
    public void Rebet_SufficientBalance_UpdatesBetToLastBet()
    {
        var service = new FlipACoinService();
        typeof(FlipACoinService).GetProperty("LastBet")?.SetValue(service, 25m);

        service.Rebet(100.0, out string? error);

        Assert.Null(error);
        Assert.Equal(25m, service.CurrentBet);
    }

    [Theory]
    [InlineData(0, false, 100.0, false)]
    [InlineData(10, false, 5.0, false)]
    [InlineData(10, false, 10.0, true)]
    [InlineData(50, true, 0.0, true)]
    public void CanFlip_EvaluatesConditionsProperly(double betAmount, bool isStreak, double balance, bool expectedResult)
    {
        var service = new FlipACoinService();
        typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, (decimal)betAmount);
        typeof(FlipACoinService).GetProperty("IsStreak")?.SetValue(service, isStreak);

        bool canFlip = service.CanFlip(balance);

        Assert.Equal(expectedResult, canFlip);
    }

    [Fact]
    public void Flip_WhenPlayerWins_DoublesPotAndContinuesStreak()
    {
        var service = new FlipACoinService();
        FlipResult result;

        do
        {
            typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 50m);
            result = service.Flip(1000.0, "heads");
        } while (!result.Won);

        Assert.Equal(100m, result.NewCurrentBet);
        Assert.True(result.IsStreak);
        Assert.False(result.LimitReached);
        Assert.Equal("Pending", result.BetStatus);
    }

    [Fact]
    public void Flip_WhenPlayerLoses_ZerosPotAndEndsStreak()
    {
        var service = new FlipACoinService();
        FlipResult result;

        do
        {
            typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 50m);
            result = service.Flip(1000.0, "heads");
        } while (result.Won);

        Assert.Equal(0m, result.NewCurrentBet);
        Assert.False(result.IsStreak);
        Assert.Equal("Lost", result.BetStatus);
        Assert.Equal(-50m, result.ResultWin);
    }

    [Fact]
    public void Flip_WhenWinHitsLimit_AutoCollectsAndEndsStreak()
    {
        var service = new FlipACoinService();
        FlipResult result;

        do
        {
            typeof(FlipACoinService).GetProperty("CurrentBet")?.SetValue(service, 6000m);
            result = service.Flip(1000.0, "heads");
        } while (!result.Won);

        Assert.True(result.LimitReached);
        Assert.True(result.ShouldCreditWin);
        Assert.Equal(12000.0, result.CreditAmount);
        Assert.Equal("Won", result.BetStatus);

        Assert.Equal(0m, service.CurrentBet);
        Assert.False(service.IsStreak);
    }
}

public class MinesServiceTests
{
    [Theory]
    [InlineData(25, 10, 10)]
    [InlineData(25, 30, 23)]
    [InlineData(36, 40, 34)]
    [InlineData(49, 50, 47)]
    public void SetGridSize_CapsMineCountToMaxMines(int requestedGridSize, int requestedMines, int expectedMines)
    {
        var service = new MinesService();

        service.SetGridSize(64);
        service.SetMineCount(requestedMines);
        service.SetGridSize(requestedGridSize);

        Assert.Equal(requestedGridSize, service.GridSize);
        Assert.Equal(expectedMines, service.MineCount);
    }

    [Fact]
    public void BettingModifiers_DoubleAndHalf_CalculateCorrectly()
    {
        var service = new MinesService();
        service.AddChip(50, 1000, out _);

        service.DoubleBet(1000, out string? errorMsgDouble);
        Assert.Null(errorMsgDouble);
        Assert.Equal(100m, service.CurrentBet);

        service.HalfBet();
        Assert.Equal(50m, service.CurrentBet);
    }

    [Fact]
    public void StartGame_ValidBet_DeductsBalanceAndInitializesGrid()
    {
        var service = new MinesService();
        service.AddChip(50, 200, out _);

        var result = service.StartGame(200);

        Assert.Equal(MinesGameState.Playing, service.GameState);
        Assert.Equal(150.0, result.BalanceAfterBet);
        Assert.Equal(50m, result.BetAmount);
        Assert.Equal(service.GridSize, service.Cells.Length);
        Assert.All(service.Cells, cell => Assert.Equal(MinesCellState.Hidden, cell));
    }

    [Fact]
    public void RevealCell_Mine_TriggersGameOverAndLoss()
    {
        var service = new MinesService();
        service.AddChip(50, 200, out _);
        service.StartGame(200);

        var mineSet = (HashSet<int>)typeof(MinesService)
            .GetField("_mineSet", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service)!;

        int mineIndex = mineSet.First();

        var result = service.RevealCell(mineIndex, 150.0);

        Assert.True(result.HitMine);
        Assert.Equal(MinesGameState.GameOver, service.GameState);
        Assert.Equal("Lost", result.BetStatus);
        Assert.Equal(150.0, result.NewBalance);
        Assert.Equal(MinesCellState.MineBoom, service.Cells[mineIndex]);
    }

    [Fact]
    public void RevealCell_SafeCell_IncreasesMultiplier_ThenCashOut()
    {
        var service = new MinesService();
        service.AddChip(10, 100, out _);
        service.StartGame(100);

        var mineSet = (HashSet<int>)typeof(MinesService)
            .GetField("_mineSet", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service)!;

        int safeIndex = Enumerable.Range(0, service.GridSize).First(i => !mineSet.Contains(i));

        var revealResult = service.RevealCell(safeIndex, 90.0);

        Assert.False(revealResult.HitMine);
        Assert.Equal(1, service.SafeRevealed);
        Assert.Equal(MinesCellState.Safe, service.Cells[safeIndex]);
        Assert.True(service.CurrentMultiplier > 1.0);

        var cashOutResult = service.CashOut(90.0);

        Assert.Equal(MinesGameState.CashedOut, service.GameState);
        Assert.True(cashOutResult.CashOutWin > 10m);
        Assert.True(cashOutResult.NewBalance > 100.0);
    }
}

public class RouletteServiceTests
{
    [Fact]
    public void PlaceBet_SufficientBalance_AddsToBetsDictionary()
    {
        var service = new RouletteService();

        service.PlaceBet("num-15", 10, 100, out string? errorMsg1);
        service.PlaceBet("red", 10, 100, out string? errorMsg2);

        Assert.Null(errorMsg1);
        Assert.Null(errorMsg2);
        Assert.True(service.Bets.ContainsKey("num-15"));
        Assert.True(service.Bets.ContainsKey("red"));
        Assert.Equal(10, service.Bets["num-15"]);
        Assert.Equal(10, service.Bets["red"]);
        Assert.Equal(20, service.TotalBet);
    }

    [Fact]
    public void PlaceBet_SameCellTwice_AccumulatesChipValue()
    {
        var service = new RouletteService();

        service.PlaceBet("black", 25, 100, out _);
        service.PlaceBet("black", 25, 100, out _);

        Assert.Equal(50, service.Bets["black"]);
        Assert.Equal(50, service.TotalBet);
    }

    [Fact]
    public void PlaceBet_InsufficientBalance_ShowsErrorAndRejectsBet()
    {
        var service = new RouletteService();

        service.PlaceBet("even", 25, 10, out string? errorMsg);

        Assert.Equal("Balanță insuficientă!", errorMsg);
        Assert.Empty(service.Bets);
    }

    [Fact]
    public void ClearBets_EmptiesDictionary()
    {
        var service = new RouletteService();
        service.PlaceBet("num-0", 5, 100, out _);
        service.PlaceBet("red", 10, 100, out _);

        service.ClearBets();

        Assert.Empty(service.Bets);
        Assert.Equal(0, service.TotalBet);
    }

    [Fact]
    public void PrepareSpin_WithoutBets_ReturnsError()
    {
        var service = new RouletteService();

        var setup = service.PrepareSpin(100.0, out string? error);

        Assert.Null(setup);
        Assert.Equal("Plasează un pariu mai întâi!", error);
    }

    [Fact]
    public void PrepareSpin_WithBets_GeneratesValidSpinSetup()
    {
        var service = new RouletteService();
        service.PlaceBet("red", 20, 100, out _);

        var setup = service.PrepareSpin(100.0, out string? error);

        Assert.Null(error);
        Assert.NotNull(setup);
        Assert.Equal(80.0, setup.BalanceAfterBet);
        Assert.Equal(20, setup.TotalBet);
        Assert.True(service.WheelOrder.Contains(setup.WinningNumber));
        Assert.NotEqual(0, setup.NewWheelDeg);
        Assert.NotEqual(0, setup.NewBallDeg);
    }

    [Theory]
    [InlineData("num-17", 17, 10, 360)]
    [InlineData("num-17", 18, 10, 0)]
    [InlineData("num-0", 0, 10, 140)]
    public void ResolveSpin_StraightUpBets_CalculatesCorrectly(string betKey, int resultNumber, int betAmount, double expectedWin)
    {
        var service = new RouletteService();
        service.PlaceBet(betKey, betAmount, 1000, out _);

        var result = service.ResolveSpin(resultNumber, 1000 - betAmount);

        Assert.Equal(expectedWin, result.TotalWin);
        Assert.Empty(service.Bets);
    }

    [Theory]
    [InlineData("red", 1, 10, 20)]
    [InlineData("black", 2, 10, 20)]
    [InlineData("even", 4, 10, 20)]
    [InlineData("odd", 7, 10, 20)]
    [InlineData("low", 10, 10, 20)]
    [InlineData("high", 20, 10, 20)]
    [InlineData("doz1", 5, 10, 30)]
    [InlineData("doz2", 15, 10, 30)]
    [InlineData("doz3", 30, 10, 30)]
    [InlineData("col1", 4, 10, 30)]
    [InlineData("col2", 5, 10, 30)]
    [InlineData("col3", 6, 10, 30)]
    public void ResolveSpin_OutsideBets_CalculatesCorrectPayouts(string betKey, int resultNumber, int betAmount, double expectedWin)
    {
        var service = new RouletteService();
        service.PlaceBet(betKey, betAmount, 1000, out _);

        var result = service.ResolveSpin(resultNumber, 1000 - betAmount);

        Assert.Equal(expectedWin, result.TotalWin);
    }

    [Fact]
    public void ResolveSpin_ResultIsZero_LosesAllOutsideBets()
    {
        var service = new RouletteService();
        service.PlaceBet("red", 10, 100, out _);
        service.PlaceBet("even", 10, 100, out _);
        service.PlaceBet("low", 10, 100, out _);
        service.PlaceBet("doz1", 10, 100, out _);
        service.PlaceBet("col1", 10, 100, out _);

        var result = service.ResolveSpin(0, 50);

        Assert.Equal(0, result.TotalWin);
        Assert.Equal("Lost", result.BetStatus);
    }
}

public class ScratchCardServiceTests
{
    [Fact]
    public void SelectCard_SufficientBalance_UpdatesSelectedCost()
    {
        var service = new ScratchCardService();

        service.SelectCard(50, 100.0);

        Assert.Equal(50, service.SelectedCost);
    }

    [Fact]
    public void SelectCard_InsufficientBalance_DoesNotUpdateCost()
    {
        var service = new ScratchCardService();

        service.SelectCard(50, 10.0);

        Assert.Equal(0, service.SelectedCost);
    }

    [Fact]
    public void BuyCard_NoCardSelected_ReturnsNullAndShowsError()
    {
        var service = new ScratchCardService();

        var result = service.BuyCard(100.0, out string? errorMsg);

        Assert.Null(result);
        Assert.Equal("Alege un bilet mai întâi!", errorMsg);
    }

    [Fact]
    public void BuyCard_InsufficientBalance_ReturnsNullAndShowsError()
    {
        var service = new ScratchCardService();
        service.SelectCard(50, 50.0);

        var result = service.BuyCard(10.0, out string? errorMsg);

        Assert.Null(result);
        Assert.Equal("Balanță insuficientă!", errorMsg);
    }

    [Fact]
    public void BuyCard_ValidTransaction_DeductsBalanceAndGeneratesGrid()
    {
        var service = new ScratchCardService();
        service.SelectCard(10, 100.0);

        var result = service.BuyCard(100.0, out string? errorMsg);

        Assert.Null(errorMsg);
        Assert.NotNull(result);
        Assert.Equal(90.0, result.NewBalance);
        Assert.Equal(10, result.Cost);

        Assert.NotNull(service.Grid);
        Assert.Equal(9, service.Grid.Length);
        Assert.All(service.Grid, cell => Assert.NotNull(cell));
    }

    [Fact]
    public void EvaluateGrid_NoWinningRows_PaysZero()
    {
        var service = new ScratchCardService();

        typeof(ScratchCardService).GetProperty("SelectedCost")?.SetValue(service, 10);

        string[] losingGrid = {
            "🍒", "🍋", "🍒",
            "🔔", "⭐", "🔔",
            "💎", "7️⃣", "💎"
        };
        typeof(ScratchCardService).GetProperty("Grid")?.SetValue(service, losingGrid);

        var result = service.EvaluateGrid();

        Assert.Equal(0.0, result.TotalWin);
        Assert.Empty(result.WinningRows);
        Assert.Equal("Lost", result.BetStatus);
    }

    [Fact]
    public void EvaluateGrid_SingleWinningRow_PaysCorrectMultiplier()
    {
        var service = new ScratchCardService();
        typeof(ScratchCardService).GetProperty("SelectedCost")?.SetValue(service, 10);

        string[] winningGrid = {
            "🍒", "🍒", "🍒",
            "🍋", "⭐", "🔔",
            "💎", "7️⃣", "💎"
        };
        typeof(ScratchCardService).GetProperty("Grid")?.SetValue(service, winningGrid);

        var result = service.EvaluateGrid();

        Assert.Equal(15.0, result.TotalWin);
        Assert.Single(result.WinningRows);
        Assert.Contains(0, result.WinningRows);
        Assert.Equal("Won", result.BetStatus);
    }

    [Fact]
    public void EvaluateGrid_MultipleWinningRows_CalculatesSumCorrectly()
    {
        var service = new ScratchCardService();
        typeof(ScratchCardService).GetProperty("SelectedCost")?.SetValue(service, 5);

        string[] multiWinGrid = {
            "🍒", "🍒", "🍒",
            "🍋", "7️⃣", "💎",
            "⭐", "⭐", "⭐"
        };
        typeof(ScratchCardService).GetProperty("Grid")?.SetValue(service, multiWinGrid);

        var result = service.EvaluateGrid();

        Assert.Equal(47.5, result.TotalWin);
        Assert.Equal(2, result.WinningRows.Count);
        Assert.Contains(0, result.WinningRows);
        Assert.Contains(2, result.WinningRows);
        Assert.Equal("Won", result.BetStatus);
    }

    [Fact]
    public void Reset_ClearsGameStateAndCost()
    {
        var service = new ScratchCardService();
        service.SelectCard(50, 100.0);
        service.BuyCard(100.0, out _);

        service.Reset();

        Assert.Equal(0, service.SelectedCost);
        Assert.Equal(9, service.Grid.Length);
        Assert.All(service.Grid, cell => Assert.Null(cell));
    }
}

public class SlotsServiceTests
{
    [Fact]
    public void GenerateSpin_InsufficientBalance_ReturnsNullAndShowsError()
    {
        var service = new SlotsService();

        var result = service.GenerateSpin(50, 10.0, out string? errorMsg);

        Assert.Null(result);
        Assert.Equal("Balantă insuficientă!", errorMsg);
    }

    [Fact]
    public void GenerateSpin_ValidBalance_DeductsBalanceAndGeneratesGrid()
    {
        var service = new SlotsService();

        var result = service.GenerateSpin(10, 100.0, out string? errorMsg);

        Assert.Null(errorMsg);
        Assert.NotNull(result);
        Assert.Equal(90.0, result.BalanceAfterBet);
        Assert.Equal(10, result.BetAmount);

        Assert.NotNull(result.FlatGrid);
        Assert.Equal(15, result.FlatGrid.Length);

        Assert.NotNull(service.ResultGrid);
        Assert.Equal(5, service.ResultGrid.GetLength(0));
        Assert.Equal(3, service.ResultGrid.GetLength(1));
    }

    [Fact]
    public void CopyResultToDisplay_TransfersGridState()
    {
        var service = new SlotsService();
        service.GenerateSpin(10, 100.0, out _);

        service.CopyResultToDisplay();

        for (int r = 0; r < 5; r++)
        {
            for (int row = 0; row < 3; row++)
            {
                Assert.Equal(service.ResultGrid[r, row], service.DisplayGrid[r, row]);
            }
        }
    }

    private static readonly int[][] TestPaylines = new[]
    {
        new[] { 0, 0, 0, 0, 0 },
        new[] { 1, 1, 1, 1, 1 },
        new[] { 2, 2, 2, 2, 2 },
        new[] { 2, 1, 0, 1, 2 },
        new[] { 0, 1, 2, 1, 0 },
    };

    public static IEnumerable<object[]> GetPayoutTestCases()
    {
        var symbols = new (string emoji, double mult3, double mult4, double mult5)[]
        {
            ("🍒", 3, 5, 10),
            ("🍋", 3, 5, 10),
            ("🍊", 3, 5, 10),
            ("⭐", 2, 5, 15),
            ("🔔", 5, 10, 25),
            ("💎", 10, 25, 50),
            ("7️⃣", 25, 100, 200)
        };

        for (int lineIndex = 0; lineIndex < 5; lineIndex++)
        {
            foreach (var sym in symbols)
            {
                yield return new object[] { lineIndex, sym.emoji, 3, sym.mult3 };
                yield return new object[] { lineIndex, sym.emoji, 4, sym.mult4 };
                yield return new object[] { lineIndex, sym.emoji, 5, sym.mult5 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetPayoutTestCases))]
    public void CalculateWins_EverySymbol_EveryLine_EveryLength_CalculatesCorrectPayout(
        int lineIndex, string targetSymbol, int matchCount, double expectedMultiplier)
    {
        var service = new SlotsService();
        int betAmount = 10;

        string[,] dummyGrid = new string[5, 3];
        for (int r = 0; r < 5; r++)
        {
            for (int row = 0; row < 3; row++)
            {
                dummyGrid[r, row] = $"X_{r}_{row}";
            }
        }

        int[] activeLine = TestPaylines[lineIndex];
        for (int c = 0; c < matchCount; c++)
        {
            dummyGrid[c, activeLine[c]] = targetSymbol;
        }

        typeof(SlotsService).GetProperty("ResultGrid")?.SetValue(service, dummyGrid);

        var result = service.CalculateWins(betAmount);

        double expectedWin = expectedMultiplier * betAmount;

        Assert.Equal(expectedWin, result.TotalWin);
        Assert.Equal("Won", result.BetStatus);
        Assert.Single(result.WinLines);
        Assert.Equal(targetSymbol, result.WinLines[0].Symbol);
        Assert.Equal(matchCount, result.WinLines[0].MatchCount);
    }

    [Fact]
    public void CalculateWins_NoMatches_ReturnsLostStatus()
    {
        var service = new SlotsService();

        string[,] dummyGrid = new string[5, 3];
        int counter = 0;
        for (int r = 0; r < 5; r++)
        {
            for (int row = 0; row < 3; row++)
            {
                dummyGrid[r, row] = $"SYM_{counter++}";
            }
        }

        typeof(SlotsService).GetProperty("ResultGrid")?.SetValue(service, dummyGrid);

        var result = service.CalculateWins(10);

        Assert.Equal(0, result.TotalWin);
        Assert.Equal("Lost", result.BetStatus);
        Assert.Empty(result.WinLines);
        Assert.Empty(result.ActivePaylines);
        Assert.Empty(result.WinCells);
    }
}

public class WheelOfFortuneServiceTests
{
    [Fact]
    public void PrepareSpin_CalculatesRotationAndReturnsValidSlot()
    {
        var service = new WheelOfFortuneService();

        var setup = service.PrepareSpin();

        Assert.InRange(setup.WinIndex, 0, service.Slots.Length - 1);
        Assert.Equal(service.Slots[setup.WinIndex], setup.WonSlot);

        Assert.True(setup.NewWheelRotation >= 2520.0);
        Assert.Equal(setup.NewWheelRotation, service.CurrentRotation);
    }

    [Fact]
    public void ResolveSpin_MysteryPrize_ReturnsNoCashPrizeAndUnchangedBalance()
    {
        var service = new WheelOfFortuneService();
        var mysterySlot = service.Slots.First(s => s.IsMystery);
        double startingBalance = 100.0;

        var result = service.ResolveSpin(mysterySlot, startingBalance);

        Assert.False(result.HasCashPrize);
        Assert.Equal(startingBalance, result.NewBalance);
        Assert.Equal("Won", result.BetStatus);
        Assert.Equal("", result.ResultMsg);
    }

    [Theory]
    [InlineData(5, 100.0, 105.0)]
    [InlineData(25, 0.0, 25.0)]
    [InlineData(1000, 50.0, 1050.0)]
    [InlineData(10000, 20.0, 10020.0)]
    public void ResolveSpin_CashPrize_UpdatesBalanceAndReturnsMessage(double cashValue, double startingBalance, double expectedBalance)
    {
        var service = new WheelOfFortuneService();

        var cashSlot = service.Slots.First(s => !s.IsMystery && s.CashValue == (decimal)cashValue);

        var result = service.ResolveSpin(cashSlot, startingBalance);

        Assert.True(result.HasCashPrize);
        Assert.Equal(expectedBalance, result.NewBalance);
        Assert.Equal("Won", result.BetStatus);

        Assert.Contains(cashValue.ToString("N0"), result.ResultMsg);
    }
}

public class LocalStorageDummyJSRuntime : IJSRuntime
{
    public string? LocalStorageMockValue { get; set; } = null;
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "localStorage.getItem" && typeof(TValue) == typeof(string))
        {
            return new ValueTask<TValue>((TValue)(object)LocalStorageMockValue!);
        }
        return new ValueTask<TValue>(default(TValue)!);
    }
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }
}