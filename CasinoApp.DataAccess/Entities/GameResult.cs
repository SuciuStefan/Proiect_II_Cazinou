namespace CasinoApp.DataAccess.Entities
{
    public class GameResult
    {
        public int Id { get; set; }
        public int BetId { get; set; }
        public int? SessionId { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public double Multiplier { get; set; }
        public double WinAmount { get; set; }
        public string? ResultData { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}