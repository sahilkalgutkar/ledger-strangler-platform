using LegacyMonolith.Models;

namespace LegacyMonolith.Tests;

// The storage round trip is proved against a real Postgres in
// PostgresRoundTripTests. These pin the invariant itself so a regression shows
// up in the fast tests instead of only in the ones that need Docker.
public class TimestampPrecisionTests
{
    private static readonly DateTime SubMicrosecond =
        new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc).AddTicks(7);

    [Fact]
    public void Account_CreatedAt_is_truncated_to_microseconds()
    {
        var account = new Account { CreatedAt = SubMicrosecond };

        Assert.Equal(0, account.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(SubMicrosecond.AddTicks(-7), account.CreatedAt);
    }

    [Fact]
    public void Statement_truncates_every_persisted_timestamp()
    {
        var statement = new Statement
        {
            PeriodStart = SubMicrosecond,
            PeriodEnd = SubMicrosecond.AddDays(30),
            GeneratedAt = SubMicrosecond,
        };

        Assert.Equal(0, statement.PeriodStart.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(0, statement.PeriodEnd.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(0, statement.GeneratedAt.Ticks % TimeSpan.TicksPerMicrosecond);
    }

    [Fact]
    public void Truncation_rounds_down_so_it_matches_what_postgres_keeps()
    {
        var justUnderTheNextMicrosecond =
            new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerMicrosecond - 1);

        var account = new Account { CreatedAt = justUnderTheNextMicrosecond };

        Assert.True(account.CreatedAt <= justUnderTheNextMicrosecond);
    }

    [Fact]
    public void An_already_microsecond_value_is_left_alone()
    {
        // The path an entity loaded from Postgres takes: it has to be a no-op,
        // or every read would shift the timestamp again.
        var fromPostgres = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerMicrosecond * 5);

        var account = new Account { CreatedAt = fromPostgres };

        Assert.Equal(fromPostgres, account.CreatedAt);
    }

    [Fact]
    public void Kind_and_offset_survive_truncation()
    {
        var account = new Account { CreatedAt = SubMicrosecond };

        Assert.Equal(DateTimeKind.Utc, account.CreatedAt.Kind);
    }
}
