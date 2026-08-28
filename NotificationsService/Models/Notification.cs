namespace NotificationsService.Models;

public class Notification
{
    private DateTimeOffset _createdAt;

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Message { get; set; } = string.Empty;

    // Truncated on the way in so the value this object carries is the value the
    // row carries. GetForAccountAsync and GetAllAsync both order by this
    // column, so it also keeps in-memory ordering consistent with what a query
    // against the database would return.
    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => _createdAt = PostgresTime.Truncate(value);
    }
}
