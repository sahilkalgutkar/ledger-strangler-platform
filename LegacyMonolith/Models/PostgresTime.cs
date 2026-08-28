namespace LegacyMonolith.Models;

/// <summary>
/// Rounds a timestamp to what PostgreSQL can actually store.
/// </summary>
/// <remarks>
/// PostgreSQL's timestamp types hold microseconds; .NET's DateTime counts
/// 100-nanosecond ticks. Anything finer is returned to a caller and then
/// dropped on the way into the row, so an entity handed back from a create
/// carries a timestamp no later read of that row can reproduce.
///
/// This is duplicated in NotificationsService rather than shared. The legacy
/// monolith deliberately references no other project in this solution - that
/// is the whole premise of the migration - and giving it one for four lines of
/// arithmetic would undo that to save nothing.
/// </remarks>
internal static class PostgresTime
{
    public static DateTime Truncate(DateTime value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
