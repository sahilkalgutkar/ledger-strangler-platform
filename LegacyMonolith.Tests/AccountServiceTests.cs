using LegacyMonolith.Data;
using LegacyMonolith.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegacyMonolith.Tests;

public class AccountServiceTests
{
    private static AccountService CreateService(out LegacyDbContext db)
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new LegacyDbContext(options);
        return new AccountService(db);
    }

    [Fact]
    public async Task CreateAccountAsync_persists_account_with_opening_balance()
    {
        var svc = CreateService(out _);

        var account = await svc.CreateAccountAsync("Jane Doe", 100m);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("Jane Doe", account.CustomerName);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public async Task GetAccountAsync_returns_null_for_unknown_id()
    {
        var svc = CreateService(out _);

        var account = await svc.GetAccountAsync(Guid.NewGuid());

        Assert.Null(account);
    }

    [Fact]
    public async Task GetAllAccountsAsync_returns_accounts_oldest_first()
    {
        var svc = CreateService(out _);
        var first = await svc.CreateAccountAsync("First", 0m);
        var second = await svc.CreateAccountAsync("Second", 0m);

        var accounts = await svc.GetAllAccountsAsync();

        Assert.Equal(new[] { first.Id, second.Id }, accounts.Select(a => a.Id));
    }

    [Fact]
    public async Task AdjustBalanceAsync_applies_positive_delta()
    {
        var svc = CreateService(out _);
        var account = await svc.CreateAccountAsync("Jane Doe", 100m);

        var updated = await svc.AdjustBalanceAsync(account.Id, 50m);

        Assert.Equal(150m, updated.Balance);
    }

    [Fact]
    public async Task AdjustBalanceAsync_applies_negative_delta_when_funds_available()
    {
        var svc = CreateService(out _);
        var account = await svc.CreateAccountAsync("Jane Doe", 100m);

        var updated = await svc.AdjustBalanceAsync(account.Id, -40m);

        Assert.Equal(60m, updated.Balance);
    }

    [Fact]
    public async Task AdjustBalanceAsync_throws_when_delta_would_go_negative()
    {
        var svc = CreateService(out _);
        var account = await svc.CreateAccountAsync("Jane Doe", 30m);

        await Assert.ThrowsAsync<InsufficientBalanceException>(
            () => svc.AdjustBalanceAsync(account.Id, -50m));
    }

    [Fact]
    public async Task AdjustBalanceAsync_throws_for_unknown_account()
    {
        var svc = CreateService(out _);

        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => svc.AdjustBalanceAsync(Guid.NewGuid(), 10m));
    }
}
