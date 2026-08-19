using AccountsService.Data;
using AccountsService.Events;
using AccountsService.Models;

namespace AccountsService.Services;

public class AccountService
{
    private readonly AccountsRepository _repository;
    private readonly IEventPublisher _events;

    public AccountService(AccountsRepository repository, IEventPublisher events)
    {
        _repository = repository;
        _events = events;
    }

    public Task<Account> CreateAccountAsync(string customerName, decimal openingBalance)
        => _repository.CreateAsync(customerName, openingBalance);

    public Task<Account?> GetAccountAsync(Guid id) => _repository.GetAsync(id);

    public Task<List<Account>> GetAllAccountsAsync() => _repository.ListAsync();

    public async Task<Account> AdjustBalanceAsync(Guid id, decimal delta)
    {
        var account = await _repository.AdjustBalanceAsync(id, delta);
        await _events.PublishBalanceChangedAsync(account.Id, account.Balance);
        return account;
    }
}
