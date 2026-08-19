using Microsoft.EntityFrameworkCore;
using NotificationsService.Data;
using NotificationsService.Models;

namespace NotificationsService.Services;

public class NotificationService
{
    private readonly NotificationsDbContext _db;

    public NotificationService(NotificationsDbContext db)
    {
        _db = db;
    }

    public async Task<Notification> RecordBalanceChangeAsync(Guid accountId, decimal newBalance, DateTimeOffset changedAtUtc)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Message = $"Balance for account {accountId} changed to {newBalance:F2}",
            CreatedAt = changedAtUtc
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<List<Notification>> GetForAccountAsync(Guid accountId)
    {
        return await _db.Notifications
            .Where(n => n.AccountId == accountId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetAllAsync()
    {
        return await _db.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }
}
