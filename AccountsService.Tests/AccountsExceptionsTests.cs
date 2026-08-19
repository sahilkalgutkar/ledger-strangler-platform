using AccountsService.Services;
using Xunit;

namespace AccountsService.Tests;

public class AccountsExceptionsTests
{
    [Fact]
    public void ConcurrentUpdateException_message_names_the_account()
    {
        var accountId = Guid.NewGuid();

        var ex = new ConcurrentUpdateException(accountId);

        Assert.Contains(accountId.ToString(), ex.Message);
    }
}
