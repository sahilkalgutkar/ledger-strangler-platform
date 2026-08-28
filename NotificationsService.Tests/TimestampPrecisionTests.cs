using NotificationsService.Models;

namespace NotificationsService.Tests;

public class TimestampPrecisionTests
{
    private static readonly DateTimeOffset SubMicrosecond =
        new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero).AddTicks(7);

    [Fact]
    public void CreatedAt_is_truncated_to_microseconds()
    {
        var notification = new Notification { CreatedAt = SubMicrosecond };

        Assert.Equal(0, notification.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(SubMicrosecond.AddTicks(-7), notification.CreatedAt);
    }

    [Fact]
    public void Truncation_rounds_down()
    {
        var justUnder = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero)
            .AddTicks(TimeSpan.TicksPerMicrosecond - 1);

        var notification = new Notification { CreatedAt = justUnder };

        Assert.True(notification.CreatedAt <= justUnder);
    }

    [Fact]
    public void An_already_microsecond_value_is_left_alone()
    {
        var fromPostgres = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero)
            .AddTicks(TimeSpan.TicksPerMicrosecond * 5);

        var notification = new Notification { CreatedAt = fromPostgres };

        Assert.Equal(fromPostgres, notification.CreatedAt);
    }

    [Fact]
    public void The_offset_survives_truncation()
    {
        var offset = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.FromHours(-5)).AddTicks(7);

        var notification = new Notification { CreatedAt = offset };

        Assert.Equal(TimeSpan.FromHours(-5), notification.CreatedAt.Offset);
    }
}
