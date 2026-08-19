using System.Text;
using System.Text.Json;
using AccountsService.Events;
using RabbitMQ.Client;
using Shared.Contracts;
using Testcontainers.RabbitMq;
using Xunit;

namespace AccountsService.Tests;

public class RabbitMqEventPublisherTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().Build();
    private RabbitMqEventPublisher _publisher = null!;
    private IConnection _consumerConnection = null!;
    private IModel _consumerChannel = null!;
    private string _queueName = null!;

    public async Task InitializeAsync()
    {
        await _rabbitMq.StartAsync();
        var connectionString = _rabbitMq.GetConnectionString();

        _publisher = new RabbitMqEventPublisher(connectionString);

        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _consumerConnection = factory.CreateConnection();
        _consumerChannel = _consumerConnection.CreateModel();
        _consumerChannel.ExchangeDeclare(AccountBalanceChangedEvent.ExchangeName, ExchangeType.Topic, durable: true);
        _queueName = _consumerChannel.QueueDeclare().QueueName;
        _consumerChannel.QueueBind(_queueName, AccountBalanceChangedEvent.ExchangeName, AccountBalanceChangedEvent.RoutingKey);
    }

    public async Task DisposeAsync()
    {
        _consumerChannel.Dispose();
        _consumerConnection.Dispose();
        _publisher.Dispose();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task PublishBalanceChangedAsync_puts_a_message_on_the_topic_exchange()
    {
        var accountId = Guid.NewGuid();

        await _publisher.PublishBalanceChangedAsync(accountId, 250m);

        var evt = await WaitForMessageAsync();
        Assert.NotNull(evt);
        Assert.Equal(accountId, evt!.AccountId);
        Assert.Equal(250m, evt.NewBalance);
    }

    private async Task<AccountBalanceChangedEvent?> WaitForMessageAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var result = _consumerChannel.BasicGet(_queueName, autoAck: true);
            if (result is not null)
            {
                var json = Encoding.UTF8.GetString(result.Body.ToArray());
                return JsonSerializer.Deserialize<AccountBalanceChangedEvent>(json);
            }

            await Task.Delay(200);
        }

        return null;
    }
}
