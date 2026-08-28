using LegacyMonolith.Data;
using LegacyMonolith.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LegacyMonolith.Tests;

/// <summary>
/// The rest of this project's tests use the EF Core in-memory provider, which
/// stores .NET objects as they are and therefore cannot observe a difference
/// between what an entity carries and what a column can hold. That is exactly
/// the difference these tests are about, so they run against a real Postgres.
/// </summary>
public class PostgresRoundTripTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private LegacyDbContext NewContext() =>
        new(new DbContextOptionsBuilder<LegacyDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options);

    [Fact]
    public async Task The_account_returned_from_a_create_matches_the_row_it_wrote()
    {
        // CreatedAt is stamped from DateTime.UtcNow inside the service, so
        // unlike the statement test below there is no way to force a value with
        // a sub-microsecond remainder. On a runner whose clock ticks in whole
        // microseconds this passes whether or not the truncation is there, so
        // treat it as confirming the round trip rather than as the thing that
        // would catch a regression - TimestampPrecisionTests does that.
        await using var writeContext = NewContext();
        var created = await new AccountService(writeContext).CreateAccountAsync("Jane Doe", 100m);

        // A fresh context on purpose: querying the one that wrote the row would
        // return the tracked instance still held in memory and compare it with
        // itself, which would pass no matter what Postgres stored.
        await using var readContext = NewContext();
        var fetched = await readContext.Accounts.SingleAsync(a => a.Id == created.Id);

        Assert.Equal(created.CreatedAt, fetched.CreatedAt);
        Assert.Equal(0, fetched.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond);
    }

    [Fact]
    public async Task The_statement_returned_from_a_generate_matches_the_row_it_wrote()
    {
        await using var writeContext = NewContext();
        var account = await new AccountService(writeContext).CreateAccountAsync("Jane Doe", 250m);

        // Deliberately carrying ticks Postgres cannot store, so this test
        // detects the mismatch every run. GeneratedAt comes from DateTime.UtcNow
        // inside the service and cannot be injected, and on a machine whose
        // clock happens to tick in whole microseconds it would agree by luck -
        // these two bounds are what make the assertion reliable rather than
        // dependent on the runner's clock granularity.
        var periodStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(7);
        var periodEnd = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc).AddTicks(3);
        var generated = await new StatementService(writeContext)
            .GenerateStatementAsync(account.Id, periodStart, periodEnd);

        await using var readContext = NewContext();
        var fetched = await readContext.Statements.SingleAsync(s => s.Id == generated.Id);

        Assert.Equal(generated.GeneratedAt, fetched.GeneratedAt);
        Assert.Equal(generated.PeriodStart, fetched.PeriodStart);
        Assert.Equal(generated.PeriodEnd, fetched.PeriodEnd);
    }
}
