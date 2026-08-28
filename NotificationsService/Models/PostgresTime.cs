namespace NotificationsService.Models;

/// <summary>
/// Rounds a timestamp to what PostgreSQL can actually store.
/// </summary>
/// <remarks>
/// PostgreSQL's timestamp types hold microseconds; .NET's DateTimeOffset counts
/// 100-nanosecond ticks. The timestamp on a notification originates as
/// DateTimeOffset.UtcNow inside AccountsService and travels here over RabbitMQ
/// as JSON, which preserves every tick - so without this, the value on the
/// event and the value in the row differ, and the notification returned from
/// recording one differs from the same notification read back afterwards.
///
/// Deliberately duplicated in LegacyMonolith rather than shared: that project
/// references nothing else in this solution on purpose.
/// </remarks>
internal static class PostgresTime
{
    public static DateTimeOffset Truncate(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
