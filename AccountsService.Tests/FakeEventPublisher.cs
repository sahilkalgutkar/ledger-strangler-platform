using AccountsService.Events;

namespace AccountsService.Tests;

public class FakeEventPublisher : IEventPublisher
{
    public List<(Guid AccountId, decimal NewBalance)> Published { get; } = new();

    public Task PublishBalanceChangedAsync(Guid accountId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        Published.Add((accountId, newBalance));
        return Task.CompletedTask;
    }
}
