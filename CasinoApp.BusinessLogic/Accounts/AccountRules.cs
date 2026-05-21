namespace CasinoApp.BusinessLogic.Accounts;

public enum LoginValidation
{
    Valid,
    MissingFields
}

public enum RegistrationValidation
{
    Valid,
    MissingFields,
    UsernameTooLong,
    PasswordTooShort,
    PasswordTooLong
}

public enum WalletValidation
{
    Valid,
    NoAuthenticatedPlayer,
    InvalidAmount,
    InsufficientFunds,
    MaximumBalanceReached,
    DepositExceedsMaximumBalance
}

public static class AccountRules
{
    public static LoginValidation ValidateLogin(string username, string password) =>
        string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
            ? LoginValidation.MissingFields
            : LoginValidation.Valid;

    public static RegistrationValidation ValidateRegistration(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return RegistrationValidation.MissingFields;
        }

        if (username.Length > 18)
            return RegistrationValidation.UsernameTooLong;

        if (password.Length < 6)
            return RegistrationValidation.PasswordTooShort;

        return password.Length > 18
            ? RegistrationValidation.PasswordTooLong
            : RegistrationValidation.Valid;
    }
}

public static class WalletRules
{
    public static WalletValidation ValidateOperation(
        bool hasAuthenticatedPlayer,
        string operation,
        double amount,
        double currentBalance,
        double maximumBalance)
    {
        if (!hasAuthenticatedPlayer)
            return WalletValidation.NoAuthenticatedPlayer;

        if (amount <= 0)
            return WalletValidation.InvalidAmount;

        if (operation == "withdraw" && amount > currentBalance)
            return WalletValidation.InsufficientFunds;

        if (operation == "deposit" && currentBalance + amount > maximumBalance)
        {
            return currentBalance >= maximumBalance
                ? WalletValidation.MaximumBalanceReached
                : WalletValidation.DepositExceedsMaximumBalance;
        }

        return WalletValidation.Valid;
    }

    public static double GetBalanceAfterOperation(string operation, double currentBalance, double amount) =>
        operation == "deposit" ? currentBalance + amount : currentBalance - amount;

    public static double GetDepositSpaceLeft(double currentBalance, double maximumBalance) =>
        maximumBalance - currentBalance;
}
