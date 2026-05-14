using CasinoApp.DataAccess.Entities;

namespace CasinoApp.Web.Services
{
    // Aceasta "cutie" tine datele playerului logat in memorie
    // AddScoped = o instanta per conexiune (per browser tab) - perfect pentru Blazor Server
    public class SessionService
    {
        public Player? CurrentPlayer { get; private set; }

        public bool IsLoggedIn => CurrentPlayer != null;

        // Apelat dupa login reusit
        public void SetPlayer(Player player)
        {
            CurrentPlayer = player;
<<<<<<< HEAD
            NotifyStateChanged();
        }

        public void UpdateBalance(double newBalance)
        {
            if (CurrentPlayer != null)
            CurrentPlayer.Balance = newBalance;
            NotifyStateChanged();
        }

        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
=======
>>>>>>> parent of a490a62 (Send_email_password)
        }

        // Apelat la logout
        public void Clear()
        {
            CurrentPlayer = null;
        }
    }
}
