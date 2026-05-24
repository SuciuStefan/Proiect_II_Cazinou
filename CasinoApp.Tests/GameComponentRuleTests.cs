using CasinoApp.DataAccess.DB_operations;
using CasinoApp.DataAccess.Entities;
using CasinoApp.Web.Components.Pages;
using CasinoApp.Web.Components.Pages.Games;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace CasinoApp.Tests;

public class BarbutComponentTests
{
    [Fact]
    public void HandleChipClick_InsufficientBalance_ShowsErrorMessageAndDoesNotUpdateBet()
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 10 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "betAmount", 0);
        ReflectionTestSupport.Invoke(component, "HandleChipClick", 25);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var betAmount = ReflectionTestSupport.GetField<int>(component, "betAmount");
        Assert.Equal("Balanță insuficientă!", errorMsg);
        Assert.Equal(0, betAmount);
    }
    [Fact]
    public void HandleChipClick_SufficientBalance_UpdatesBetAndClearsError()
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 100 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "betAmount", 0);
        ReflectionTestSupport.SetField(component, "errorMsg", "error");
        ReflectionTestSupport.Invoke(component, "HandleChipClick", 25);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var betAmount = ReflectionTestSupport.GetField<int>(component, "betAmount");
        Assert.Equal("", errorMsg);
        Assert.Equal(25, betAmount);
    }
    [Fact]
    public void ClearBet_ResetsBetAmountAndStatus()
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        ReflectionTestSupport.SetField(component, "betAmount", 50);
        ReflectionTestSupport.SetField(component, "showResult", true);
        ReflectionTestSupport.SetField(component, "errorMsg", "error");
        ReflectionTestSupport.Invoke(component, "ClearBet");
        var betAmount = ReflectionTestSupport.GetField<int>(component, "betAmount");
        var showResult = ReflectionTestSupport.GetField<bool>(component, "showResult");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal(0, betAmount);
        Assert.False(showResult);
        Assert.Equal("", errorMsg);
    }
    [Fact]
    public void Roll_WithZeroBet_DoesNotStartRoll()
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        ReflectionTestSupport.SetField(component, "betAmount", 0);
        ReflectionTestSupport.SetField(component, "isRolling", false);
        ReflectionTestSupport.Invoke(component, "Roll");
        var isRolling = ReflectionTestSupport.GetField<bool>(component, "isRolling");
        Assert.False(isRolling);
    }
    [Fact]
    public void Roll_InsufficientBalance_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 10 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "betAmount", 50);
        ReflectionTestSupport.Invoke(component, "Roll");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var isRolling = ReflectionTestSupport.GetField<bool>(component, "isRolling");
        Assert.Equal("Balanță insuficientă!", errorMsg);
        Assert.False(isRolling);
    }
    [Theory]
    [InlineData(3, 100.0, 1100.0)]
    [InlineData(2, 50.0, 1050.0)]
    [InlineData(1, 0.0, 1000.0)]
    [InlineData(0, -50.0, 950.0)]
    public void OnRollComplete_UpdatesBalanceCorrectly_BasedOnBeatCount(int beatCount, double netGain, double expectedBalance)
    {
        using var database = new TemporaryDatabase();
        var component = new Barbut();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = 950 });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player);
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetField(component, "betAmount", 50);
        ReflectionTestSupport.SetField(component, "netGain", netGain);
        ReflectionTestSupport.SetField(component, "beatCount", beatCount);
        ReflectionTestSupport.SetField(component, "currentBetId", null);
        ReflectionTestSupport.Invoke(component, "OnRollComplete");
        var finalBalance = sessionService.CurrentPlayer.Balance;
        Assert.Equal(expectedBalance, finalBalance);
    }
}

public class BlackjackComponentTests
{
    [Fact]
    public void AddChip_InsufficientBalance_ShowsErrorMessageAndDoesNotUpdateBet()
    {
        using var database = new TemporaryDatabase();
        var component = new Blackjack();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 10 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "currentBet", 0m);
        ReflectionTestSupport.Invoke(component, "AddChip", 25);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal("Balanta insuficienta!", errorMsg);
        Assert.Equal(0m, currentBet);
    }
    [Fact]
    public void AddChip_SufficientBalance_UpdatesBetAndClearsError()
    {
        using var database = new TemporaryDatabase();
        var component = new Blackjack();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 100 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "currentBet", 0m);
        ReflectionTestSupport.SetField(component, "errorMsg", "existing error");
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.Invoke(component, "AddChip", 25);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal("", errorMsg);
        Assert.Equal(25m, currentBet);
    }
    [Fact]
    public void ClearBet_ResetsBetAmountAndError()
    {
        using var database = new TemporaryDatabase();
        var component = new Blackjack();
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.SetField(component, "errorMsg", "some error");
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.Invoke(component, "ClearBet");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal(0m, currentBet);
        Assert.Equal("", errorMsg);
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
        var method = typeof(Blackjack).GetMethod("ComputeHandResult", BindingFlags.NonPublic | BindingFlags.Static);
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
}

public class FlipACoinComponentTests
{
    [Fact]
    public void PickChoice_ValidChoice_UpdatesPlayerChoiceAndClearsError()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.SetField(component, "errorMsg", "existing error");
        ReflectionTestSupport.Invoke(component, "PickChoice", "heads");
        var choice = ReflectionTestSupport.GetField<string>(component, "playerChoice");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal("heads", choice);
        Assert.Equal("", errorMsg);
    }
    [Fact]
    public void AddChip_InsufficientBalance_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 10 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "currentBet", 0m);
        ReflectionTestSupport.SetField(component, "isStreak", false);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.Invoke(component, "AddChip", 25);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal("Balanta insuficienta!", errorMsg);
        Assert.Equal(0m, currentBet);
    }
    [Fact]
    public void AddChip_DuringStreak_ShowsStreakErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 1000 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.SetField(component, "isStreak", true);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.Invoke(component, "AddChip", 10);
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal("Ești pe dublaj! Dă FLIP sau apasă pe încasare.", errorMsg);
        Assert.Equal(50m, currentBet);
    }
    [Fact]
    public void ClearBet_NormalState_ResetsBetAmount()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.SetField(component, "isStreak", false);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.Invoke(component, "ClearBet");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal(0m, currentBet);
    }
    [Fact]
    public void ClearBet_DuringStreak_CashesOutAndUpdatesBalance()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = 100.0 });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player);
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetField(component, "currentBet", 150m);
        ReflectionTestSupport.SetField(component, "isStreak", true);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.SetField(component, "currentBetId", null);
        ReflectionTestSupport.Invoke(component, "ClearBet");
        var finalBalance = sessionService.CurrentPlayer.Balance;
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        var isStreak = ReflectionTestSupport.GetField<bool>(component, "isStreak");
        Assert.Equal(250.0, finalBalance);
        Assert.Equal(0m, currentBet);
        Assert.False(isStreak);
    }
    [Fact]
    public void Rebet_SufficientBalance_UpdatesBetToLastBet()
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 100 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "lastBet", 25m);
        ReflectionTestSupport.SetField(component, "currentBet", 0m);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        ReflectionTestSupport.SetField(component, "isStreak", false);
        ReflectionTestSupport.Invoke(component, "Rebet");
        var currentBet = ReflectionTestSupport.GetField<decimal>(component, "currentBet");
        Assert.Equal(25m, currentBet);
    }
    [Theory]
    [InlineData(0, "heads", false, false)]
    [InlineData(10, "", false, false)]
    [InlineData(10, "heads", false, true)]
    [InlineData(50, "tails", true, true)]
    public void CanFlip_EvaluatesConditionsProperly(
      double betAmount, string choice, bool streak, bool expectedResult)
    {
        using var database = new TemporaryDatabase();
        var component = new FlipACoin();
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(new Player { Id = 1, Username = "Test", Balance = 100 });
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetField(component, "currentBet", (decimal)betAmount);
        ReflectionTestSupport.SetField(component, "playerChoice", choice);
        ReflectionTestSupport.SetField(component, "isStreak", streak);
        ReflectionTestSupport.SetField(component, "isResetting", false);
        var method = component.GetType().GetMethod("CanFlip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canFlip = (bool)method!.Invoke(component, null)!;
        Assert.Equal(expectedResult, canFlip);
    }
}

public class MinesComponentTests
{
    private (Mines component, CasinoApp.Web.Services.SessionService session) SetupComponent(double initialBalance)
    {
        var component = new Mines();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = initialBalance });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player!);
        var gameRepo = new GameRepository();
        var game = gameRepo.GetByName("Mines");
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetMember(component, "GameRepo", gameRepo);
        if (game != null)
        {
            ReflectionTestSupport.SetField(component, "minesGameId", game.Id);
        }
        return (component, sessionService);
    }
    [Theory]
    [InlineData(25, 10, 10)]
    [InlineData(25, 30, 23)]
    [InlineData(36, 40, 34)]
    [InlineData(49, 50, 47)]
    public void SetGridSize_CapsMineCountToMaxMines(int requestedGridSize, int requestedMines, int expectedMines)
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        ReflectionTestSupport.SetField(component, "mineCount", requestedMines);
        ReflectionTestSupport.Invoke(component, "SetGridSize", requestedGridSize);
        Assert.Equal(requestedGridSize, ReflectionTestSupport.GetField<int>(component, "gridSize"));
        Assert.Equal(expectedMines, ReflectionTestSupport.GetField<int>(component, "mineCount"));
    }
    [Fact]
    public void BettingModifiers_DoubleAndHalf_CalculateCorrectly()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(1000);
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.Invoke(component, "DoubleBet");
        Assert.Equal(100m, ReflectionTestSupport.GetField<decimal>(component, "currentBet"));
        ReflectionTestSupport.Invoke(component, "HalfBet");
        Assert.Equal(50m, ReflectionTestSupport.GetField<decimal>(component, "currentBet"));
    }
    [Fact]
    public void StartGame_ValidBet_DeductsBalanceAndPlacesMines()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(200);
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.SetField(component, "gridSize", 25);
        ReflectionTestSupport.Invoke(component, "StartGame");
        Assert.Equal("Playing", ReflectionTestSupport.GetField<object>(component, "gameState").ToString());
        Assert.Equal(150.0, session.CurrentPlayer!.Balance);
    }
    [Fact]
    public void RevealCell_Mine_TriggersGameOver()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(200);
        ReflectionTestSupport.SetField(component, "currentBet", 50m);
        ReflectionTestSupport.SetField(component, "gridSize", 25);
        ReflectionTestSupport.Invoke(component, "StartGame");
        var stateAfterStart = ReflectionTestSupport.GetField<object>(component, "gameState").ToString();
        Assert.Equal("Playing", stateAfterStart);
        var fixedMines = new HashSet<int> { 10 };
        ReflectionTestSupport.SetField(component, "mineSet", fixedMines);
        ReflectionTestSupport.Invoke(component, "RevealCell", 10);
        var finalState = ReflectionTestSupport.GetField<object>(component, "gameState").ToString();
        Assert.Equal("GameOver", finalState);
    }
}

public class RouletteComponentTests
{
    private (Roulette component, CasinoApp.Web.Services.SessionService session) SetupComponent(double initialBalance)
    {
        var component = new Roulette();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = initialBalance });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player!);
        var gameRepo = new GameRepository();
        var game = gameRepo.GetByName("Roulette");
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetMember(component, "GameRepo", gameRepo);
        if (game != null)
        {
            ReflectionTestSupport.SetField(component, "rouletteGameId", game.Id);
        }
        return (component, sessionService);
    }
    #region 1. Betting Mechanics Tests
    [Fact]
    public void PlaceBet_SufficientBalance_AddsToBetsDictionary()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        ReflectionTestSupport.SetField(component, "selectedChip", 10);
        ReflectionTestSupport.SetField(component, "bets", new Dictionary<string, int>());
        ReflectionTestSupport.SetField(component, "isSpinning", false);
        ReflectionTestSupport.Invoke(component, "PlaceBet", "num-15");
        ReflectionTestSupport.Invoke(component, "PlaceBet", "red");
        var bets = ReflectionTestSupport.GetField<Dictionary<string, int>>(component, "bets");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal("", errorMsg);
        Assert.True(bets.ContainsKey("num-15"));
        Assert.True(bets.ContainsKey("red"));
        Assert.Equal(10, bets["num-15"]);
        Assert.Equal(10, bets["red"]);
    }
    [Fact]
    public void PlaceBet_SameCellTwice_AccumulatesChipValue()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        ReflectionTestSupport.SetField(component, "selectedChip", 25);
        ReflectionTestSupport.SetField(component, "bets", new Dictionary<string, int>());
        ReflectionTestSupport.Invoke(component, "PlaceBet", "black");
        ReflectionTestSupport.Invoke(component, "PlaceBet", "black");
        var bets = ReflectionTestSupport.GetField<Dictionary<string, int>>(component, "bets");
        Assert.Equal(50, bets["black"]);
    }
    [Fact]
    public void PlaceBet_InsufficientBalance_ShowsErrorAndRejectsBet()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(10);
        ReflectionTestSupport.SetField(component, "selectedChip", 25);
        ReflectionTestSupport.SetField(component, "bets", new Dictionary<string, int>());
        ReflectionTestSupport.Invoke(component, "PlaceBet", "even");
        var bets = ReflectionTestSupport.GetField<Dictionary<string, int>>(component, "bets");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal("Balanță insuficientă!", errorMsg);
        Assert.Empty(bets);
    }
    [Fact]
    public void ClearBets_EmptiesDictionary()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        var existingBets = new Dictionary<string, int> { { "num-0", 5 }, { "red", 10 } };
        ReflectionTestSupport.SetField(component, "bets", existingBets);
        ReflectionTestSupport.Invoke(component, "ClearBets");
        var bets = ReflectionTestSupport.GetField<Dictionary<string, int>>(component, "bets");
        Assert.Empty(bets);
    }
    #endregion
    #region 2. Payout Engine Tests (CalculateWins)
    [Theory]
    [InlineData("num-17", 17, 10, 360)]
    [InlineData("num-17", 18, 10, 0)]
    [InlineData("num-0", 0, 10, 140)]
    public void CalculateWins_StraightUpBets_CalculatesCorrectly(string betKey, int resultNumber, int betAmount, double expectedWin)
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        var bets = new Dictionary<string, int> { { betKey, betAmount } };
        ReflectionTestSupport.SetField(component, "bets", bets);
        ReflectionTestSupport.SetField(component, "lastResult", resultNumber);
        ReflectionTestSupport.Invoke(component, "CalculateWins");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        Assert.Equal(expectedWin, totalWin);
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
    public void CalculateWins_OutsideBets_CalculatesCorrectPayouts(string betKey, int resultNumber, int betAmount, double expectedWin)
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        var bets = new Dictionary<string, int> { { betKey, betAmount } };
        ReflectionTestSupport.SetField(component, "bets", bets);
        ReflectionTestSupport.SetField(component, "lastResult", resultNumber);
        ReflectionTestSupport.Invoke(component, "CalculateWins");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        Assert.Equal(expectedWin, totalWin);
    }
    [Fact]
    public void CalculateWins_ResultIsZero_LosesAllOutsideBets()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100);
        var bets = new Dictionary<string, int>
    {
      { "red", 10 },
      { "even", 10 },
      { "low", 10 },
      { "doz1", 10 },
      { "col1", 10 }
    };
        ReflectionTestSupport.SetField(component, "bets", bets);
        ReflectionTestSupport.SetField(component, "lastResult", 0);
        ReflectionTestSupport.Invoke(component, "CalculateWins");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        Assert.Equal(0, totalWin);
    }
    [Fact]
    public void CalculateWins_WinningCombination_UpdatesPlayerBalance()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        var bets = new Dictionary<string, int> { { "red", 50 } };
        ReflectionTestSupport.SetField(component, "bets", bets);
        ReflectionTestSupport.SetField(component, "lastResult", 9);
        ReflectionTestSupport.Invoke(component, "CalculateWins");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        Assert.Equal(100, totalWin);
        Assert.Equal(200.0, session.CurrentPlayer!.Balance);
    }
    #endregion
}

public class ScratchCardComponentTests
{
    private (ScratchCard component, CasinoApp.Web.Services.SessionService session) SetupComponent(double initialBalance)
    {
        var component = new ScratchCard();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = initialBalance });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player!);
        var gameRepo = new GameRepository();
        var game = gameRepo.GetByName("Scratch Cards");
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetMember(component, "GameRepo", gameRepo);
        ReflectionTestSupport.SetMember(component, "JS", new DummyJSRuntime());
        if (game != null)
        {
            ReflectionTestSupport.SetField(component, "scratchGameId", game.Id);
        }
        return (component, sessionService);
    }
    [Fact]
    public void SelectCard_SufficientBalance_UpdatesSelectedCost()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100.0);
        ReflectionTestSupport.Invoke(component, "SelectCard", 50);
        var selectedCost = ReflectionTestSupport.GetField<int>(component, "selectedCost");
        Assert.Equal(50, selectedCost);
    }
    [Fact]
    public void BuyCard_NoCardSelected_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 0);
        ReflectionTestSupport.Invoke(component, "BuyCard");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal("Alege un bilet mai întâi!", errorMsg);
    }
    [Fact]
    public void BuyCard_InsufficientBalance_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(10.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 50);
        ReflectionTestSupport.Invoke(component, "BuyCard");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.Equal("Balanță insuficientă!", errorMsg);
    }
    [Fact]
    public void BuyCard_ValidTransaction_DeductsBalanceAndGeneratesGrid()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 10);
        ReflectionTestSupport.Invoke(component, "BuyCard");
        var gameState = ReflectionTestSupport.GetField<object>(component, "gameState").ToString();
        var grid = ReflectionTestSupport.GetField<string[]>(component, "grid");
        Assert.Equal("Scratching", gameState);
        Assert.Equal(90.0, session.CurrentPlayer!.Balance);
        Assert.NotNull(grid);
        Assert.Equal(9, grid.Length);
    }
    [Fact]
    public void CalculateResult_NoWinningRows_PaysZero()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 10);
        ReflectionTestSupport.SetField(component, "totalWin", 0.0);
        ReflectionTestSupport.SetField(component, "winningRows", new List<int>());
        string[] losingGrid = {
      "🍒", "🍋", "🍒",
      "🔔", "⭐", "🔔",
      "💎", "7️⃣", "💎"
    };
        ReflectionTestSupport.SetField(component, "grid", losingGrid);
        ReflectionTestSupport.Invoke(component, "CalculateResult");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        var winningRows = ReflectionTestSupport.GetField<List<int>>(component, "winningRows");
        Assert.Equal(0.0, totalWin);
        Assert.Empty(winningRows);
        Assert.Equal(100.0, session.CurrentPlayer!.Balance);
    }
    [Fact]
    public void CalculateResult_SingleWinningRow_PaysCorrectMultiplier()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 10);
        string[] winningGrid = {
      "🍒", "⭐", "🍒",
      "🍋", "🍋", "🍋",
      "💎", "7️⃣", "💎"
    };
        ReflectionTestSupport.SetField(component, "grid", winningGrid);
        ReflectionTestSupport.Invoke(component, "CalculateResult");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        var winningRows = ReflectionTestSupport.GetField<List<int>>(component, "winningRows");
        Assert.Equal(20.0, totalWin);
        Assert.Single(winningRows);
        Assert.Contains(1, winningRows);
        Assert.Equal(120.0, session.CurrentPlayer!.Balance);
    }
    [Fact]
    public void CalculateResult_MultipleWinningRows_CalculatesSumCorrectly()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 5);
        string[] multiWinGrid = {
      "🍒", "🍒", "🍒",
      "🍋", "7️⃣", "💎",
      "⭐", "⭐", "⭐"
    };
        ReflectionTestSupport.SetField(component, "grid", multiWinGrid);
        ReflectionTestSupport.Invoke(component, "CalculateResult");
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        var winningRows = ReflectionTestSupport.GetField<List<int>>(component, "winningRows");
        Assert.Equal(47.5, totalWin);
        Assert.Equal(2, winningRows.Count);
        Assert.Contains(0, winningRows);
        Assert.Contains(2, winningRows);
        Assert.Equal(147.5, session.CurrentPlayer!.Balance);
    }
    [Fact]
    public void PlayAgain_ResetsGameStateAndCost()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "selectedCost", 50);
        ReflectionTestSupport.SetField(component, "errorMsg", "Some old error");
        ReflectionTestSupport.SetField(component, "gameState", Enum.Parse(typeof(ScratchCard).GetNestedType("GameState", System.Reflection.BindingFlags.NonPublic)!, "Result"));
        ReflectionTestSupport.Invoke(component, "PlayAgain");
        var selectedCost = ReflectionTestSupport.GetField<int>(component, "selectedCost");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var gameState = ReflectionTestSupport.GetField<object>(component, "gameState").ToString();
        Assert.Equal(0, selectedCost);
        Assert.Equal("", errorMsg);
        Assert.Equal("SelectCard", gameState);
    }
}
public class DummyJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
      => new ValueTask<TValue>(default(TValue)!);
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
      => new ValueTask<TValue>(default(TValue)!);
}

public class SlotsComponentTests
{
    private (Slots component, CasinoApp.Web.Services.SessionService session) SetupComponent(double initialBalance)
    {
        var component = new Slots();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = initialBalance });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player!);
        var gameRepo = new GameRepository();
        var game = gameRepo.GetByName("Slots");
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetMember(component, "GameRepo", gameRepo);
        ReflectionTestSupport.SetMember(component, "JS", new DummyJSRuntime());
        if (game != null)
        {
            ReflectionTestSupport.SetField(component, "slotsGameId", game.Id);
        }
        return (component, sessionService);
    }
    #region 1. Betting & Engine Mechanics
    [Fact]
    public void SetBet_ValidInput_UpdatesBetAmount()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "isSpinning", false);
        ReflectionTestSupport.Invoke(component, "SetBet", 25);
        var betAmount = ReflectionTestSupport.GetField<int>(component, "betAmount");
        Assert.Equal(25, betAmount);
    }
    [Fact]
    public void Spin_InsufficientBalance_ShowsErrorMessageAndRejectsSpin()
    {
        using var database = new TemporaryDatabase();
        var (component, _) = SetupComponent(10.0);
        ReflectionTestSupport.SetField(component, "betAmount", 50);
        ReflectionTestSupport.SetField(component, "isSpinning", false);
        ReflectionTestSupport.Invoke(component, "Spin");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        var isSpinning = ReflectionTestSupport.GetField<bool>(component, "isSpinning");
        Assert.Equal("Balantă insuficientă!", errorMsg);
        Assert.False(isSpinning);
    }
    [Fact]
    public void Spin_ValidBalance_DeductsBalanceAndGeneratesGrid()
    {
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "betAmount", 10);
        ReflectionTestSupport.SetField(component, "isSpinning", false);
        ReflectionTestSupport.Invoke(component, "Spin");
        var isSpinning = ReflectionTestSupport.GetField<bool>(component, "isSpinning");
        var resultGrid = ReflectionTestSupport.GetField<string[,]>(component, "resultGrid");
        var errorMsg = ReflectionTestSupport.GetField<string>(component, "errorMsg");
        Assert.True(isSpinning);
        Assert.Equal("", errorMsg);
        Assert.Equal(90.0, session.CurrentPlayer!.Balance);
        Assert.NotNull(resultGrid);
        Assert.Equal(5, resultGrid.GetLength(0));
        Assert.Equal(3, resultGrid.GetLength(1));
    }
    #endregion
    #region 2. Payout Math (Data-Driven Test for all 105 possibilities)
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
        using var database = new TemporaryDatabase();
        var (component, session) = SetupComponent(100.0);
        int betAmount = 10;
        ReflectionTestSupport.SetField(component, "betAmount", betAmount);
        string[,] dummyGrid = new string[5, 3];
        for (int c = 0; c < 5; c++)
        {
            for (int r = 0; r < 3; r++)
            {
                dummyGrid[c, r] = $"X_{c}_{r}";
            }
        }
        int[] activeLine = TestPaylines[lineIndex];
        for (int c = 0; c < matchCount; c++)
        {
            dummyGrid[c, activeLine[c]] = targetSymbol;
        }
        ReflectionTestSupport.SetField(component, "resultGrid", dummyGrid);
        ReflectionTestSupport.Invoke(component, "CalculateWins");
        double expectedWin = expectedMultiplier * betAmount;
        var totalWin = ReflectionTestSupport.GetField<double>(component, "totalWin");
        Assert.Equal(expectedWin, totalWin);
        Assert.Equal(100.0 + expectedWin, session.CurrentPlayer!.Balance);
    }
    #endregion
}

public class WheelOfFortuneComponentTests
{
    private (WheelOfFortune component, CasinoApp.Web.Services.SessionService session, LocalStorageDummyJSRuntime jsRuntime) SetupComponent(double initialBalance)
    {
        var component = new WheelOfFortune();
        var playerRepo = new PlayerRepository();
        playerRepo.Create(new Player { Username = "TestUser", Email = "test@gmail.com", Password = "123", Balance = initialBalance });
        var player = playerRepo.GetByUsername("TestUser");
        var sessionService = new CasinoApp.Web.Services.SessionService();
        sessionService.SetPlayer(player!);
        var gameRepo = new GameRepository();
        var game = gameRepo.GetByName("Wheel of Fortune");
        var dummyJs = new LocalStorageDummyJSRuntime();
        ReflectionTestSupport.SetMember(component, "Session", sessionService);
        ReflectionTestSupport.SetMember(component, "PlayerRepo", playerRepo);
        ReflectionTestSupport.SetMember(component, "BetRepo", new BetRepository());
        ReflectionTestSupport.SetMember(component, "GameRepo", gameRepo);
        ReflectionTestSupport.SetMember(component, "JS", dummyJs);
        if (game != null)
        {
            ReflectionTestSupport.SetField(component, "wheelGameId", game.Id);
        }
        return (component, sessionService, dummyJs);
    }
    [Fact]
    public async Task CheckSpinStatus_WhenLocalStorageMatchesToday_SetsHasSpunTodayToTrue()
    {
        using var database = new TemporaryDatabase();
        var (component, _, jsRuntime) = SetupComponent(100.0);
        jsRuntime.LocalStorageMockValue = DateTime.Today.ToString("yyyy-MM-dd");
        var method = component.GetType().GetMethod("CheckSpinStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task)method!.Invoke(component, null)!;
        await task;
        var hasSpunToday = ReflectionTestSupport.GetField<bool>(component, "hasSpunToday");
        Assert.True(hasSpunToday);
    }
    [Fact]
    public async Task CheckSpinStatus_WhenLocalStorageIsEmpty_AllowsSpin()
    {
        using var database = new TemporaryDatabase();
        var (component, _, jsRuntime) = SetupComponent(100.0);
        jsRuntime.LocalStorageMockValue = null;
        var method = component.GetType().GetMethod("CheckSpinStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task)method!.Invoke(component, null)!;
        await task;
        var hasSpunToday = ReflectionTestSupport.GetField<bool>(component, "hasSpunToday");
        Assert.False(hasSpunToday);
    }
    [Fact]
    public void Spin_WhenHasSpunTodayIsTrue_RejectsSpin()
    {
        using var database = new TemporaryDatabase();
        var (component, _, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "hasSpunToday", true);
        ReflectionTestSupport.SetField(component, "isSpinning", false);
        ReflectionTestSupport.Invoke(component, "Spin");
        var isSpinning = ReflectionTestSupport.GetField<bool>(component, "isSpinning");
        Assert.False(isSpinning);
    }
    [Fact]
    public void Spin_WhenAlreadySpinning_RejectsSubsequentClicks()
    {
        using var database = new TemporaryDatabase();
        var (component, _, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "hasSpunToday", false);
        ReflectionTestSupport.SetField(component, "isSpinning", true);
        ReflectionTestSupport.Invoke(component, "Spin");
        var hasSpunToday = ReflectionTestSupport.GetField<bool>(component, "hasSpunToday");
        Assert.False(hasSpunToday);
    }
    [Fact]
    public void CloseReveal_ResetsUIStates()
    {
        using var database = new TemporaryDatabase();
        var (component, _, _) = SetupComponent(100.0);
        ReflectionTestSupport.SetField(component, "revealPhase", 4);
        ReflectionTestSupport.SetField(component, "showResult", true);
        ReflectionTestSupport.Invoke(component, "CloseReveal");
        var revealPhase = ReflectionTestSupport.GetField<int>(component, "revealPhase");
        var showResult = ReflectionTestSupport.GetField<bool>(component, "showResult");
        Assert.Equal(0, revealPhase);
        Assert.False(showResult);
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