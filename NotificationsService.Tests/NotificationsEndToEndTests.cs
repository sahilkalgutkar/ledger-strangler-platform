using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;
using Shared.Contracts;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace NotificationsService.Tests;

/// <summary>
/// Drives the whole loop through real infrastructure: publish an event to a real
/// RabbitMQ broker exactly like AccountsService would, let the actual
/// BalanceChangedConsumer hosted service pick it up, and confirm it lands in a
/// real Postgres database, visible through the real HTTP API.
/// </summary>
public class NotificationsEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().Build();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Notifications", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:ConnectionString", _rabbitMq.GetConnectionString());
        });
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task An_event_published_to_rabbitmq_shows_up_as_a_notification_via_the_api()
    {
        // Force the host (and its BalanceChangedConsumer hosted service) to start.
        await _client.GetAsync("/");

        var accountId = Guid.NewGuid();
        PublishBalanceChangedEvent(accountId, 777.25m);

        var notifications = await PollUntilNotEmptyAsync(accountId);

        var single = Assert.Single(notifications);
        Assert.Equal(accountId, single.GetProperty("accountId").GetGuid());
        Assert.Contains("777.25", single.GetProperty("message").GetString());
    }

    private void PublishBalanceChangedEvent(Guid accountId, decimal newBalance)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(AccountBalanceChangedEvent.ExchangeName, ExchangeType.Topic, durable: true);

        var evt = new AccountBalanceChangedEvent(accountId, newBalance, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        channel.BasicPublish(AccountBalanceChangedEvent.ExchangeName, AccountBalanceChangedEvent.RoutingKey, body: body);
    }

    private async Task<List<JsonElement>> PollUntilNotEmptyAsync(Guid accountId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetFromJsonAsync<List<JsonElement>>($"/notifications/{accountId}");
            if (response is { Count: > 0 })
            {
                return response;
            }

            await Task.Delay(300);
        }

        return new List<JsonElement>();
    }
}
