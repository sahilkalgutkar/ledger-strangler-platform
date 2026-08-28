namespace LegacyMonolith.Models;

public class Account
{
    private DateTime _createdAt;

    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    // Truncated on the way in so the value this object carries is the value the
    // row carries. See PostgresTime for why.
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = PostgresTime.Truncate(value);
    }
}
