using CasinoApp.DataAccess.Entities;

namespace CasinoApp.Web.Services
{
    public class SessionService
    {
        public Player? CurrentPlayer { get; private set; }

        public bool IsLoggedIn => CurrentPlayer != null;

        public event Action? OnChange;

        public void SetPlayer(Player player)
        {
            CurrentPlayer = player;
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
        }

        public void Clear()
        {
            CurrentPlayer = null;
            NotifyStateChanged();
        }
    }
}