namespace LegacyMonolith.Models;

public class Statement
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime GeneratedAt { get; set; }
}
