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
        }

        // Apelat la logout
        public void Clear()
        {
            CurrentPlayer = null;
        }
    }
}
