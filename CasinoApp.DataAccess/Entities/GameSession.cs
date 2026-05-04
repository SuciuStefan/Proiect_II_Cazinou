namespace CasinoApp.DataAccess.Entities
{
    public class GameSession
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public string StartedAt { get; set; } = string.Empty;
        public string? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}