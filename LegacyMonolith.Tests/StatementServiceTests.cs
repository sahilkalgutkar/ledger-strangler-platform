using LegacyMonolith.Data;
using LegacyMonolith.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegacyMonolith.Tests;

public class StatementServiceTests
{
    private static (AccountService accounts, StatementService statements) CreateServices()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new LegacyDbContext(options);
        return (new AccountService(db), new StatementService(db));
    }

    [Fact]
    public async Task GenerateStatementAsync_snapshots_current_balance()
    {
        var (accounts, statements) = CreateServices();
        var account = await accounts.CreateAccountAsync("Jane Doe", 250m);
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);

        var statement = await statements.GenerateStatementAsync(account.Id, start, end);

        Assert.Equal(account.Id, statement.AccountId);
        Assert.Equal(250m, statement.ClosingBalance);
        Assert.Equal(start, statement.PeriodStart);
        Assert.Equal(end, statement.PeriodEnd);
    }

    [Fact]
    public async Task GenerateStatementAsync_throws_for_unknown_account()
    {
        var (_, statements) = CreateServices();

        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => statements.GenerateStatementAsync(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public async Task GenerateStatementAsync_rejects_inverted_period()
    {
        var (accounts, statements) = CreateServices();
        var account = await accounts.CreateAccountAsync("Jane Doe", 100m);
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 1, 1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => statements.GenerateStatementAsync(account.Id, start, end));
    }

    [Fact]
    public async Task GetStatementsAsync_returns_newest_first()
    {
        var (accounts, statements) = CreateServices();
        var account = await accounts.CreateAccountAsync("Jane Doe", 100m);
        var first = await statements.GenerateStatementAsync(account.Id, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-30));
        var second = await statements.GenerateStatementAsync(account.Id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var result = await statements.GetStatementsAsync(account.Id);

        Assert.Equal(new[] { second.Id, first.Id }, result.Select(s => s.Id));
    }

    [Fact]
    public async Task GetStatementsAsync_returns_empty_list_for_account_with_no_statements()
    {
        var (accounts, statements) = CreateServices();
        var account = await accounts.CreateAccountAsync("Jane Doe", 100m);

        var result = await statements.GetStatementsAsync(account.Id);

        Assert.Empty(result);
    }
}
