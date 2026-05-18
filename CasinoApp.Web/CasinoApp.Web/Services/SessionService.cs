using CasinoApp.DataAccess.Entities;

namespace CasinoApp.Web.Services
{
    public class SessionService
    {
        // Hard cap distribuit prin toate jocurile automat
        public const double MaxBalance = 9_999_999_999.0;

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
                CurrentPlayer.Balance = Math.Min(newBalance, MaxBalance);
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
