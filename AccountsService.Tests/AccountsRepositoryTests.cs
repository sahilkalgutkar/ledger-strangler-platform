using AccountsService.Data;
using AccountsService.Services;
using Testcontainers.Cassandra;
using Xunit;

namespace AccountsService.Tests;

public class AccountsRepositoryTests : IAsyncLifetime
{
    private readonly CassandraContainer _cassandra = new CassandraBuilder().Build();
    private AccountsRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _cassandra.StartAsync();
        var session = CassandraSessionFactory.Connect(_cassandra.Hostname, _cassandra.GetMappedPublicPort(9042), "ledger_test");
        _repository = AccountsRepository.Create(session);
    }

    public async Task DisposeAsync() => await _cassandra.DisposeAsync();

    [Fact]
    public async Task CreateAsync_then_GetAsync_round_trips_the_account()
    {
        var account = await _repository.CreateAsync("Jane Doe", 100m);

        var fetched = await _repository.GetAsync(account.Id);

        Assert.NotNull(fetched);
        Assert.Equal(account.Id, fetched!.Id);
        Assert.Equal("Jane Doe", fetched.CustomerName);
        Assert.Equal(100m, fetched.Balance);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var fetched = await _repository.GetAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task ListAsync_includes_created_accounts()
    {
        var account = await _repository.CreateAsync("List Me", 10m);

        var all = await _repository.ListAsync();

        Assert.Contains(all, a => a.Id == account.Id);
    }

    [Fact]
    public async Task AdjustBalanceAsync_applies_the_delta_via_a_lightweight_transaction()
    {
        var account = await _repository.CreateAsync("Jane Doe", 100m);

        var updated = await _repository.AdjustBalanceAsync(account.Id, 25m);

        Assert.Equal(125m, updated.Balance);
        var reFetched = await _repository.GetAsync(account.Id);
        Assert.Equal(125m, reFetched!.Balance);
    }

    [Fact]
    public async Task AdjustBalanceAsync_throws_when_it_would_go_negative()
    {
        var account = await _repository.CreateAsync("Jane Doe", 30m);

        await Assert.ThrowsAsync<InsufficientBalanceException>(
            () => _repository.AdjustBalanceAsync(account.Id, -50m));
    }

    [Fact]
    public async Task AdjustBalanceAsync_throws_for_unknown_account()
    {
        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => _repository.AdjustBalanceAsync(Guid.NewGuid(), 10m));
    }

    [Fact]
    public async Task Concurrent_adjustments_are_serialized_by_the_lightweight_transaction()
    {
        var account = await _repository.CreateAsync("Concurrent", 1000m);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _repository.AdjustBalanceAsync(account.Id, 10m))
            .ToArray();
        await Task.WhenAll(tasks);

        var final = await _repository.GetAsync(account.Id);
        Assert.Equal(1100m, final!.Balance);
    }
}
