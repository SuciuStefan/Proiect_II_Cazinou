using CasinoApp.BusinessLogic.Accounts;

namespace CasinoApp.Tests;

public class LoginRulesTests
{
    [Fact]
    public void LoginRequiresBothUsernameAndPassword()
    {
        Assert.Equal(LoginValidation.MissingFields, AccountRules.ValidateLogin("", "secret"));
        Assert.Equal(LoginValidation.MissingFields, AccountRules.ValidateLogin("player", " "));
        Assert.Equal(LoginValidation.Valid, AccountRules.ValidateLogin("player", "secret"));
    }
}

public class RegistrationRulesTests
{
    [Theory]
    [InlineData("", "player@example.com", "secret", RegistrationValidation.MissingFields)]
    [InlineData("1234567890123456789", "player@example.com", "secret", RegistrationValidation.UsernameTooLong)]
    [InlineData("player", "player@example.com", "short", RegistrationValidation.PasswordTooShort)]
    [InlineData("player", "player@example.com", "1234567890123456789", RegistrationValidation.PasswordTooLong)]
    [InlineData("player", "player@example.com", "secret", RegistrationValidation.Valid)]
    public void RegistrationValidatesFieldsBeforeRepositoryChecks(
        string username,
        string email,
        string password,
        RegistrationValidation expected)
    {
        Assert.Equal(expected, AccountRules.ValidateRegistration(username, email, password));
    }
}

public class WalletRulesTests
{
    [Theory]
    [InlineData(false, "deposit", 10, 100, WalletValidation.NoAuthenticatedPlayer)]
    [InlineData(true, "deposit", 0, 100, WalletValidation.InvalidAmount)]
    [InlineData(true, "withdraw", 101, 100, WalletValidation.InsufficientFunds)]
    [InlineData(true, "deposit", 2, 999, WalletValidation.DepositExceedsMaximumBalance)]
    [InlineData(true, "deposit", 1, 1000, WalletValidation.MaximumBalanceReached)]
    [InlineData(true, "withdraw", 100, 100, WalletValidation.Valid)]
    public void WalletOperationValidationCoversDepositsAndWithdrawals(
        bool hasPlayer,
        string operation,
        double amount,
        double currentBalance,
        WalletValidation expected)
    {
        Assert.Equal(expected, WalletRules.ValidateOperation(hasPlayer, operation, amount, currentBalance, 1000));
    }

    [Fact]
    public void WalletOperationCalculatesNewBalances()
    {
        Assert.Equal(125, WalletRules.GetBalanceAfterOperation("deposit", 100, 25));
        Assert.Equal(75, WalletRules.GetBalanceAfterOperation("withdraw", 100, 25));
    }
}
