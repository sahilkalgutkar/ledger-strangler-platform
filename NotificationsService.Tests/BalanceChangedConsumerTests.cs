using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationsService.Consumers;
using NotificationsService.Data;
using NotificationsService.Services;
using Shared.Contracts;
using Xunit;

namespace NotificationsService.Tests;

public class BalanceChangedConsumerTests
{
    private static (BalanceChangedConsumer consumer, IServiceProvider services) CreateConsumer()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<NotificationService>();
        var provider = services.BuildServiceProvider();

        var consumer = new BalanceChangedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new BalanceChangedConsumerOptions("amqp://unused"));

        return (consumer, provider);
    }

    [Fact]
    public async Task HandleMessageAsync_records_a_notification_from_the_event_payload()
    {
        var (consumer, services) = CreateConsumer();
        var evt = new AccountBalanceChangedEvent(Guid.NewGuid(), 42.5m, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        await consumer.HandleMessageAsync(body);

        using var scope = services.CreateScope();
        var notifications = await scope.ServiceProvider.GetRequiredService<NotificationService>().GetForAccountAsync(evt.AccountId);
        var single = Assert.Single(notifications);
        Assert.Equal(evt.AccountId, single.AccountId);
        Assert.Contains("42.50", single.Message);
    }

    [Fact]
    public async Task HandleMessageAsync_ignores_a_payload_that_does_not_deserialize()
    {
        var (consumer, services) = CreateConsumer();

        await consumer.HandleMessageAsync(Encoding.UTF8.GetBytes("null"));

        using var scope = services.CreateScope();
        var all = await scope.ServiceProvider.GetRequiredService<NotificationService>().GetAllAsync();
        Assert.Empty(all);
    }
}
