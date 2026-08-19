namespace AccountsService.Models;

public record Account(Guid Id, string CustomerName, decimal Balance, DateTimeOffset CreatedAt);
