using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationsService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;

namespace NotificationsService.Consumers;

public record BalanceChangedConsumerOptions(string ConnectionString);

/// <summary>
/// Consumes AccountBalanceChangedEvent off the ledger.events exchange and turns
/// each one into a notification record. Uses a fresh DI scope per message since
/// NotificationService depends on a scoped DbContext, but the consumer itself is
/// a long-lived singleton hosted service.
/// </summary>
public class BalanceChangedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BalanceChangedConsumerOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public BalanceChangedConsumer(IServiceScopeFactory scopeFactory, BalanceChangedConsumerOptions options)
    {
        _scopeFactory = scopeFactory;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_options.ConnectionString), DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(AccountBalanceChangedEvent.ExchangeName, ExchangeType.Topic, durable: true);

        var queueName = _channel.QueueDeclare(
            queue: "notifications.balance-changed",
            durable: true,
            exclusive: false,
            autoDelete: false).QueueName;
        _channel.QueueBind(queueName, AccountBalanceChangedEvent.ExchangeName, AccountBalanceChangedEvent.RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await HandleMessageAsync(ea.Body.ToArray());
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public async Task HandleMessageAsync(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        var evt = JsonSerializer.Deserialize<AccountBalanceChangedEvent>(json);
        if (evt is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
        await notifications.RecordBalanceChangeAsync(evt.AccountId, evt.NewBalance, evt.ChangedAtUtc);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // RabbitMQ.Client's auto-recovering connection can throw from Close() if a
        // recovery attempt is racing shutdown - this is teardown, not a case worth
        // failing the host over.
        try { _channel?.Close(); } catch (Exception) { }
        try { _connection?.Close(); } catch (Exception) { }
        await base.StopAsync(cancellationToken);
    }
}
