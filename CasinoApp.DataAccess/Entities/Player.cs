namespace CasinoApp.DataAccess.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public double Balance { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}