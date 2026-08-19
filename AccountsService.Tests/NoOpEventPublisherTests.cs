using AccountsService.Events;
using Xunit;

namespace AccountsService.Tests;

public class NoOpEventPublisherTests
{
    [Fact]
    public async Task PublishBalanceChangedAsync_completes_without_doing_anything()
    {
        var publisher = new NoOpEventPublisher();

        await publisher.PublishBalanceChangedAsync(Guid.NewGuid(), 42m);
    }
}
