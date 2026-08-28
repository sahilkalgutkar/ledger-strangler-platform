namespace AccountsService.Models;

public record Account(Guid Id, string CustomerName, decimal Balance, DateTimeOffset CreatedAt)
{
    private readonly DateTimeOffset _createdAt = TruncateToMilliseconds(CreatedAt);

    // Cassandra's `timestamp` type is a millisecond count since the epoch, while
    // DateTimeOffset carries 100-nanosecond ticks. Anything finer than a
    // millisecond therefore survives being returned to a caller but not being
    // stored, so without this the CreatedAt handed back by CreateAsync is a value
    // no subsequent GetAsync can ever reproduce - the two disagree by up to a
    // millisecond, and ListAsync's ordering turns non-deterministic for accounts
    // created within the same one.
    //
    // This is an init accessor rather than an auto-property initializer so that
    // it also runs for `with` expressions, which assign through the accessor but
    // never re-run a property initializer. That puts the truncation on every path
    // an Account can be built by: a create, a row read back, or a copy. Reading a
    // row through it is a no-op, because Cassandra already rounded.
    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        init => _createdAt = TruncateToMilliseconds(value);
    }

    private static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMillisecond));
}
