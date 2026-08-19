namespace AccountsService.Services;

public class AccountNotFoundException : Exception
{
    public AccountNotFoundException(Guid accountId) : base($"Account {accountId} was not found")
    {
    }
}

public class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException(Guid accountId, decimal balance, decimal requestedDelta)
        : base($"Account {accountId} has balance {balance}, cannot apply delta {requestedDelta}")
    {
    }
}

/// <summary>
/// The Cassandra lightweight transaction backing a balance adjustment lost a race
/// against a concurrent writer too many times in a row.
/// </summary>
public class ConcurrentUpdateException : Exception
{
    public ConcurrentUpdateException(Guid accountId)
        : base($"Could not apply balance update for account {accountId} - too much contention")
    {
    }
}
