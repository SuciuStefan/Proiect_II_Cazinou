namespace CasinoApp.DataAccess.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}