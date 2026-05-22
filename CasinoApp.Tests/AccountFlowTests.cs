using CasinoApp.DataAccess.DB_operations;
using CasinoApp.DataAccess.Entities;
using CasinoApp.Web.Components.Pages;
using Xunit;

namespace CasinoApp.Tests;

public class ResetPasswordComponentTests
{
    [Fact]
    public void ResetPasswordNow_WithEmptyPassword_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ResetPassword();

        ReflectionTestSupport.SetField(component, "TokenRepo", new PasswordResetTokenRepository());
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "newPassword", "");

        ReflectionTestSupport.Invoke(component, "ResetPasswordNow");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        var messageClass = ReflectionTestSupport.GetField<string>(component, "messageClass");

        Assert.Equal("Introdu parola nouă.", message);
        Assert.Equal("message error", messageClass);
    }

    [Fact]
    public void ResetPasswordNow_WithInvalidOrExpiredToken_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ResetPassword();
        ReflectionTestSupport.SetField(component, "TokenRepo", new PasswordResetTokenRepository());
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "newPassword", "ValidPass123");
        ReflectionTestSupport.SetField(component, "confirmPassword", "ValidPass123");

        ReflectionTestSupport.SetMember(component, "Token", "fake-invalid-token");

        ReflectionTestSupport.Invoke(component, "ResetPasswordNow");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        var messageClass = ReflectionTestSupport.GetField<string>(component, "messageClass");

        Assert.Equal("Link invalid sau expirat.", message);
        Assert.Equal("message error", messageClass);
    }

    [Fact]
    public void ResetPasswordNow_WithShortPassword_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ResetPassword();
        ReflectionTestSupport.SetField(component, "TokenRepo", new PasswordResetTokenRepository());
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "newPassword", "12345");
        ReflectionTestSupport.Invoke(component, "ResetPasswordNow");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        var messageClass = ReflectionTestSupport.GetField<string>(component, "messageClass");

        Assert.Equal("Parola trebuie să aibă cel puțin 6 caractere.", message);
        Assert.Equal("message error", messageClass);
    }

    [Fact]
    public void ResetPasswordNow_WithMismatchedPasswords_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ResetPassword();
        ReflectionTestSupport.SetField(component, "TokenRepo", new PasswordResetTokenRepository());
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "newPassword", "ValidPass123");
        ReflectionTestSupport.SetField(component, "confirmPassword", "DifferentPass");
        ReflectionTestSupport.Invoke(component, "ResetPasswordNow");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        var messageClass = ReflectionTestSupport.GetField<string>(component, "messageClass");

        Assert.Equal("Parolele nu coincid.", message);
        Assert.Equal("message error", messageClass);
    }
}

public class ForgotPasswordComponentTests
{
    [Fact]
    public void SendResetEmail_WithEmptyEmail_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ForgotPassword();

        ReflectionTestSupport.SetField(component, "email", "");
        ReflectionTestSupport.Invoke(component, "SendResetEmail");

        var message = ReflectionTestSupport.GetField<string>(component, "message");

        Assert.Equal("Introdu o adresă de email.", message);
    }

    [Fact]
    public void SendResetEmail_WithMalformedEmail_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ForgotPassword();
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "email", "aaaaaaaaa");
        ReflectionTestSupport.Invoke(component, "SendResetEmail");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        Assert.Equal("Formatul emailului este invalid.", message);
    }

    [Fact]
    public void SendResetEmail_WithNonGmailDomain_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ForgotPassword();
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "email", "jucator@yahoo.com");
        ReflectionTestSupport.Invoke(component, "SendResetEmail");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        Assert.Equal("Te rugăm să introduci o adresă de gmail", message);
    }

    [Fact]
    public void SendResetEmail_WithOnlyGmailDomainAndNoName_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new ForgotPassword();
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "email", "@gmail.com");
        ReflectionTestSupport.Invoke(component, "SendResetEmail");

        var message = ReflectionTestSupport.GetField<string>(component, "message");
        Assert.Equal("Formatul emailului este invalid.", message);
    }

    [Fact]
    public void SendResetEmail_WithNonExistentEmail_ShowsGenericMessageToPreventEnumeration()
    {
        using var database = new TemporaryDatabase();
        var component = new ForgotPassword();
        ReflectionTestSupport.SetField(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "email", "doesnotexist@gmail.com");
        ReflectionTestSupport.Invoke(component, "SendResetEmail");
        var message = ReflectionTestSupport.GetField<string>(component, "message");

        Assert.Equal("Dacă emailul există, vei primi un link de resetare.", message);
    }
}

public class LoginRegistrationComponentTests
{
    [Fact]
    public void HandleLogin_WithEmptyFields_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "loginUsername", "");
        ReflectionTestSupport.SetField(component, "loginPassword", "");

        ReflectionTestSupport.Invoke(component, "HandleLogin");

        var message = ReflectionTestSupport.GetField<string>(component, "loginMessage");
        var successStatus = ReflectionTestSupport.GetField<bool>(component, "loginSuccess");

        Assert.Equal("Completează toate câmpurile!", message);
        Assert.False(successStatus);
    }

    [Fact]
    public void HandleLogin_WithNonExistentUser_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());
        ReflectionTestSupport.SetField(component, "loginUsername", "GhostUser");
        ReflectionTestSupport.SetField(component, "loginPassword", "DoesntMatter");

        ReflectionTestSupport.Invoke(component, "HandleLogin");

        var message = ReflectionTestSupport.GetField<string>(component, "loginMessage");
        Assert.Equal("Utilizator sau parolă greșite.", message);
    }

    [Fact]
    public void HandleRegister_WithEmptyFields_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "");
        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Completează toate câmpurile!", message);
    }

    [Fact]
    public void HandleRegister_WithNonGmailDomain_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "ValidUser");
        ReflectionTestSupport.SetField(component, "regPassword", "ValidPass123");
        ReflectionTestSupport.SetField(component, "regEmail", "jucator@yahoo.com");

        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Pentru crearea contului este permisă doar o adresă Gmail validă.", message);
    }

    [Fact]
    public void HandleRegister_WithOnlyGmailDomainAndNoName_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "ValidUser");
        ReflectionTestSupport.SetField(component, "regPassword", "ValidPass123");
        ReflectionTestSupport.SetField(component, "regEmail", "@gmail.com");

        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Pentru crearea contului este permisă doar o adresă Gmail validă.", message);
    }

    [Fact]
    public void HandleRegister_WithUsernameTooLong_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "ThisUsernameIsWayTooLong");
        ReflectionTestSupport.SetField(component, "regEmail", "test@gmail.com");
        ReflectionTestSupport.SetField(component, "regPassword", "ValidPass123");

        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Username-ul poate avea maxim 18 caractere.", message);
    }

    [Fact]
    public void HandleRegister_WithPasswordTooShort_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "ValidUser");
        ReflectionTestSupport.SetField(component, "regEmail", "test@gmail.com");
        ReflectionTestSupport.SetField(component, "regPassword", "12345");

        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Parola trebuie să aibă minim 6 caractere.", message);
    }

    [Fact]
    public void HandleRegister_WithPasswordTooLong_ShowsErrorMessage()
    {
        using var database = new TemporaryDatabase();
        var component = new Login();
        ReflectionTestSupport.SetMember(component, "PlayerRepo", new PlayerRepository());

        ReflectionTestSupport.SetField(component, "regUsername", "ValidUser");
        ReflectionTestSupport.SetField(component, "regEmail", "test@gmail.com");
        ReflectionTestSupport.SetField(component, "regPassword", "ThisPasswordIsWayTooLongToStore");

        ReflectionTestSupport.Invoke(component, "HandleRegister");

        var message = ReflectionTestSupport.GetField<string>(component, "regMessage");
        Assert.Equal("Parola poate avea maxim 18 caractere.", message);
    }
}