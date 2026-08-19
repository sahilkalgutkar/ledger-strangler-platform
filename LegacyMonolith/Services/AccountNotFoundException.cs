namespace LegacyMonolith.Services;

public class AccountNotFoundException : Exception
{
    public AccountNotFoundException(Guid accountId) : base($"Account {accountId} was not found")
    {
    }
}
