using Microsoft.EntityFrameworkCore;
using NotificationsService.Data;
using NotificationsService.Services;
using Testcontainers.PostgreSql;

namespace NotificationsService.Tests;

/// <summary>
/// The other tests here use the EF Core in-memory provider, which cannot see a
/// difference between what an entity holds and what a column can store. This
/// one runs against a real Postgres, which is the only place that difference
/// exists.
/// </summary>
public class PostgresRoundTripTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private NotificationsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options);

    [Fact]
    public async Task The_notification_returned_from_recording_matches_the_row_it_wrote()
    {
        // The timestamp arrives on a RabbitMQ event as DateTimeOffset.UtcNow,
        // carrying ticks Postgres cannot store. Constructing it that way here
        // keeps the test honest about where the value comes from.
        var changedAt = DateTimeOffset.UtcNow;

        await using var writeContext = NewContext();
        var recorded = await new NotificationService(writeContext)
            .RecordBalanceChangeAsync(Guid.NewGuid(), 777.25m, changedAt);

        // A fresh context, so this is a real query rather than the tracked
        // instance the write left behind.
        await using var readContext = NewContext();
        var fetched = await readContext.Notifications.SingleAsync(n => n.Id == recorded.Id);

        Assert.Equal(recorded.CreatedAt, fetched.CreatedAt);
        Assert.Equal(0, fetched.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond);
    }
}
