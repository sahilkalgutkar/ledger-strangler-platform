namespace LegacyMonolith.Services;

public class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException(Guid accountId, decimal balance, decimal requestedDelta)
        : base($"Account {accountId} has balance {balance}, cannot apply delta {requestedDelta}")
    {
    }
}
