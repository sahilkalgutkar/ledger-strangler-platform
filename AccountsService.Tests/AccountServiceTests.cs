using AccountsService.Data;
using AccountsService.Services;
using Testcontainers.Cassandra;
using Xunit;

namespace AccountsService.Tests;

public class AccountServiceTests : IAsyncLifetime
{
    private readonly CassandraContainer _cassandra = new CassandraBuilder().Build();
    private FakeEventPublisher _events = null!;
    private AccountService _service = null!;

    public async Task InitializeAsync()
    {
        await _cassandra.StartAsync();
        var session = CassandraSessionFactory.Connect(_cassandra.Hostname, _cassandra.GetMappedPublicPort(9042), "ledger_service_test");
        _events = new FakeEventPublisher();
        _service = new AccountService(AccountsRepository.Create(session), _events);
    }

    public async Task DisposeAsync() => await _cassandra.DisposeAsync();

    [Fact]
    public async Task AdjustBalanceAsync_publishes_an_event_after_a_successful_adjustment()
    {
        var account = await _service.CreateAccountAsync("Jane Doe", 100m);

        await _service.AdjustBalanceAsync(account.Id, 50m);

        var published = Assert.Single(_events.Published);
        Assert.Equal(account.Id, published.AccountId);
        Assert.Equal(150m, published.NewBalance);
    }

    [Fact]
    public async Task AdjustBalanceAsync_does_not_publish_when_the_adjustment_is_rejected()
    {
        var account = await _service.CreateAccountAsync("Jane Doe", 10m);

        await Assert.ThrowsAsync<InsufficientBalanceException>(
            () => _service.AdjustBalanceAsync(account.Id, -50m));

        Assert.Empty(_events.Published);
    }
}
