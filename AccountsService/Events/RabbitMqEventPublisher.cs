using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Contracts;

namespace AccountsService.Events;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(AccountBalanceChangedEvent.ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishBalanceChangedAsync(Guid accountId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        var evt = new AccountBalanceChangedEvent(accountId, newBalance, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2; // persistent

        _channel.BasicPublish(
            exchange: AccountBalanceChangedEvent.ExchangeName,
            routingKey: AccountBalanceChangedEvent.RoutingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
