using Microsoft.EntityFrameworkCore;
using NotificationsService.Data;
using NotificationsService.Services;
using Xunit;

namespace NotificationsService.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NotificationService(new NotificationsDbContext(options));
    }

    [Fact]
    public async Task RecordBalanceChangeAsync_persists_a_readable_message()
    {
        var svc = CreateService();
        var accountId = Guid.NewGuid();

        var notification = await svc.RecordBalanceChangeAsync(accountId, 150.5m, DateTimeOffset.UtcNow);

        Assert.Equal(accountId, notification.AccountId);
        Assert.Contains("150.50", notification.Message);
    }

    [Fact]
    public async Task GetForAccountAsync_only_returns_that_accounts_notifications()
    {
        var svc = CreateService();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        await svc.RecordBalanceChangeAsync(accountA, 10m, DateTimeOffset.UtcNow);
        await svc.RecordBalanceChangeAsync(accountB, 20m, DateTimeOffset.UtcNow);

        var result = await svc.GetForAccountAsync(accountA);

        var single = Assert.Single(result);
        Assert.Equal(accountA, single.AccountId);
    }

    [Fact]
    public async Task GetForAccountAsync_returns_newest_first()
    {
        var svc = CreateService();
        var accountId = Guid.NewGuid();
        var first = await svc.RecordBalanceChangeAsync(accountId, 10m, DateTimeOffset.UtcNow.AddMinutes(-5));
        var second = await svc.RecordBalanceChangeAsync(accountId, 20m, DateTimeOffset.UtcNow);

        var result = await svc.GetForAccountAsync(accountId);

        Assert.Equal(new[] { second.Id, first.Id }, result.Select(n => n.Id));
    }

    [Fact]
    public async Task GetAllAsync_returns_every_notification()
    {
        var svc = CreateService();
        await svc.RecordBalanceChangeAsync(Guid.NewGuid(), 10m, DateTimeOffset.UtcNow);
        await svc.RecordBalanceChangeAsync(Guid.NewGuid(), 20m, DateTimeOffset.UtcNow);

        var result = await svc.GetAllAsync();

        Assert.Equal(2, result.Count);
    }
}
