using AccountsService.Models;
using AccountsService.Services;
using CassandraSession = Cassandra.ISession;
using Cassandra;

namespace AccountsService.Data;

/// <summary>
/// Balance adjustments go through a Cassandra lightweight transaction
/// (UPDATE ... IF balance = ?) instead of a blind write, since Cassandra has no
/// cross-partition ACID transactions - this is how you get compare-and-swap
/// semantics on a single partition without a distributed lock.
/// </summary>
public class AccountsRepository
{
    private const int MaxOptimisticRetries = 10;

    private readonly CassandraSession _session;
    private readonly PreparedStatement _insert;
    private readonly PreparedStatement _selectById;
    private readonly PreparedStatement _selectAll;
    private readonly PreparedStatement _updateBalanceIfMatch;

    private AccountsRepository(
        CassandraSession session,
        PreparedStatement insert,
        PreparedStatement selectById,
        PreparedStatement selectAll,
        PreparedStatement updateBalanceIfMatch)
    {
        _session = session;
        _insert = insert;
        _selectById = selectById;
        _selectAll = selectAll;
        _updateBalanceIfMatch = updateBalanceIfMatch;
    }

    public static AccountsRepository Create(CassandraSession session)
    {
        var insert = session.Prepare(
            "INSERT INTO accounts (id, customer_name, balance, created_at) VALUES (?, ?, ?, ?)");
        var selectById = session.Prepare(
            "SELECT id, customer_name, balance, created_at FROM accounts WHERE id = ?");
        var selectAll = session.Prepare(
            "SELECT id, customer_name, balance, created_at FROM accounts");
        var updateBalanceIfMatch = session.Prepare(
            "UPDATE accounts SET balance = ? WHERE id = ? IF balance = ?");

        return new AccountsRepository(session, insert, selectById, selectAll, updateBalanceIfMatch);
    }

    public async Task<Account> CreateAsync(string customerName, decimal openingBalance)
    {
        var account = new Account(Guid.NewGuid(), customerName, openingBalance, DateTimeOffset.UtcNow);
        await _session.ExecuteAsync(_insert.Bind(account.Id, account.CustomerName, account.Balance, account.CreatedAt));
        return account;
    }

    public async Task<Account?> GetAsync(Guid id)
    {
        var rowSet = await _session.ExecuteAsync(_selectById.Bind(id));
        var row = rowSet.FirstOrDefault();
        return row is null ? null : MapRow(row);
    }

    public async Task<List<Account>> ListAsync()
    {
        var rowSet = await _session.ExecuteAsync(_selectAll.Bind());
        return rowSet.Select(MapRow).OrderBy(a => a.CreatedAt).ToList();
    }

    public async Task<Account> AdjustBalanceAsync(Guid id, decimal delta)
    {
        for (var attempt = 0; attempt < MaxOptimisticRetries; attempt++)
        {
            var current = await GetAsync(id) ?? throw new AccountNotFoundException(id);
            var newBalance = current.Balance + delta;
            if (newBalance < 0)
            {
                throw new InsufficientBalanceException(id, current.Balance, delta);
            }

            var result = await _session.ExecuteAsync(_updateBalanceIfMatch.Bind(newBalance, id, current.Balance));
            var applied = result.First().GetValue<bool>("[applied]");
            if (applied)
            {
                return current with { Balance = newBalance };
            }

            // Lost the race - back off before retrying so a burst of concurrent
            // writers on the same account spreads out instead of all retrying in
            // lockstep and losing again. Random jitter, growing with each attempt.
            await Task.Delay(Random.Shared.Next(5, 20) * (attempt + 1));
        }

        throw new ConcurrentUpdateException(id);
    }

    private static Account MapRow(Row row) => new(
        row.GetValue<Guid>("id"),
        row.GetValue<string>("customer_name"),
        row.GetValue<decimal>("balance"),
        row.GetValue<DateTimeOffset>("created_at"));
}
