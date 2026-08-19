using LegacyMonolith.Data;
using LegacyMonolith.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyMonolith.Services;

public class StatementService
{
    private readonly LegacyDbContext _db;

    public StatementService(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<Statement> GenerateStatementAsync(Guid accountId, DateTime periodStart, DateTime periodEnd)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new AccountNotFoundException(accountId);

        if (periodEnd < periodStart)
        {
            throw new ArgumentException("periodEnd must not be before periodStart");
        }

        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ClosingBalance = account.Balance,
            GeneratedAt = DateTime.UtcNow
        };

        _db.Statements.Add(statement);
        await _db.SaveChangesAsync();
        return statement;
    }

    public async Task<List<Statement>> GetStatementsAsync(Guid accountId)
    {
        return await _db.Statements
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.GeneratedAt)
            .ToListAsync();
    }
}
