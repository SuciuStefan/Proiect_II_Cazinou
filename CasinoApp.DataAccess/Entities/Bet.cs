namespace CasinoApp.DataAccess.Entities
{
    public class Bet
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public int? SessionId { get; set; }
        public double Amount { get; set; }
        public string BetTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}