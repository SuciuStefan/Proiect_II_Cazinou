namespace CasinoApp.DataAccess.Entities
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double MinBet { get; set; }
        public double MaxBet { get; set; }
        public bool IsActive { get; set; }
    }
}