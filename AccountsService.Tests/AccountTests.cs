using AccountsService.Models;

namespace AccountsService.Tests;

// These run without Cassandra on purpose. The round-trip test in
// AccountsRepositoryTests proves the stored value matches, but it needs a
// container to say so; this pins the invariant itself, so a regression is
// caught by the fast tests rather than only by the slow ones.
public class AccountTests
{
    [Fact]
    public void CreatedAt_is_truncated_to_the_precision_Cassandra_stores()
    {
        // 100-nanosecond ticks that Cassandra's millisecond timestamp cannot hold.
        var precise = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero).AddTicks(9_876);

        var account = new Account(Guid.NewGuid(), "Jane Doe", 100m, precise);

        Assert.Equal(0, account.CreatedAt.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(precise.AddTicks(-9_876), account.CreatedAt);
    }

    [Fact]
    public void CreatedAt_truncates_downwards_so_it_never_precedes_the_stored_row()
    {
        var precise = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero).AddTicks(TimeSpan.TicksPerMillisecond - 1);

        var account = new Account(Guid.NewGuid(), "Jane Doe", 100m, precise);

        Assert.True(account.CreatedAt <= precise, "truncation must round down, matching what Cassandra does");
    }

    [Fact]
    public void An_already_millisecond_value_is_left_alone()
    {
        // This is the path a row read back from Cassandra takes, so it has to be
        // a no-op or every read would shift the timestamp again.
        var fromCassandra = new DateTimeOffset(2026, 8, 28, 12, 0, 0, 123, TimeSpan.Zero);

        var account = new Account(Guid.NewGuid(), "Jane Doe", 100m, fromCassandra);

        Assert.Equal(fromCassandra, account.CreatedAt);
    }

    // `with` assigns through the init accessor but never re-runs a property
    // initializer, so this is the case the accessor form exists to cover.
    [Fact]
    public void A_with_expression_is_truncated_too()
    {
        var account = new Account(Guid.NewGuid(), "Jane Doe", 100m, DateTimeOffset.UtcNow);
        var precise = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero).AddTicks(4_321);

        var copied = account with { CreatedAt = precise };

        Assert.Equal(0, copied.CreatedAt.Ticks % TimeSpan.TicksPerMillisecond);
    }

    [Fact]
    public void Adjusting_the_balance_keeps_the_created_timestamp_intact()
    {
        var account = new Account(Guid.NewGuid(), "Jane Doe", 100m, DateTimeOffset.UtcNow);

        var adjusted = account with { Balance = 150m };

        Assert.Equal(account.CreatedAt, adjusted.CreatedAt);
    }
}
