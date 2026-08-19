using LegacyMonolith.Data;
using LegacyMonolith.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyMonolith.Services;

public class AccountService
{
    private readonly LegacyDbContext _db;

    public AccountService(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<Account> CreateAccountAsync(string customerName, decimal openingBalance)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Balance = openingBalance,
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task<Account?> GetAccountAsync(Guid accountId)
    {
        return await _db.Accounts.FindAsync(accountId);
    }

    public async Task<List<Account>> GetAllAccountsAsync()
    {
        return await _db.Accounts.OrderBy(a => a.CreatedAt).ToListAsync();
    }

    public async Task<Account> AdjustBalanceAsync(Guid accountId, decimal delta)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new AccountNotFoundException(accountId);

        var newBalance = account.Balance + delta;
        if (newBalance < 0)
        {
            throw new InsufficientBalanceException(accountId, account.Balance, delta);
        }

        account.Balance = newBalance;
        await _db.SaveChangesAsync();
        return account;
    }
}
