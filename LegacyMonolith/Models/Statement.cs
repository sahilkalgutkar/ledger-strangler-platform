namespace LegacyMonolith.Models;

public class Statement
{
    private DateTime _periodStart;
    private DateTime _periodEnd;
    private DateTime _generatedAt;

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal ClosingBalance { get; set; }

    // All three are persisted, so all three are truncated. The period bounds
    // come in off a request rather than from a clock, but a caller that sends
    // sub-microsecond precision would hit exactly the same mismatch on read.
    public DateTime PeriodStart
    {
        get => _periodStart;
        set => _periodStart = PostgresTime.Truncate(value);
    }

    public DateTime PeriodEnd
    {
        get => _periodEnd;
        set => _periodEnd = PostgresTime.Truncate(value);
    }

    public DateTime GeneratedAt
    {
        get => _generatedAt;
        set => _generatedAt = PostgresTime.Truncate(value);
    }
}
