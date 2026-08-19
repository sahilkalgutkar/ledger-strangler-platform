namespace AccountsService.Events;

public interface IEventPublisher
{
    Task PublishBalanceChangedAsync(Guid accountId, decimal newBalance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Placeholder until the RabbitMQ publisher lands - lets AccountsService ship and
/// be fully testable before the event bus exists.
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishBalanceChangedAsync(Guid accountId, decimal newBalance, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
