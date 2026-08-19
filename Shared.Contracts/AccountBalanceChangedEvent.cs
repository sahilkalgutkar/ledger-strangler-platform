namespace Shared.Contracts;

/// <summary>
/// Published by AccountsService whenever a balance adjustment is applied.
/// This is the contract both sides of the event bus compile against, so the
/// publisher and consumer can't silently drift out of sync on shape.
/// </summary>
public record AccountBalanceChangedEvent(Guid AccountId, decimal NewBalance, DateTimeOffset ChangedAtUtc)
{
    public const string ExchangeName = "ledger.events";
    public const string RoutingKey = "account.balance-changed";
}
