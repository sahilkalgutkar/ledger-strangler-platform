namespace NotificationsService.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
