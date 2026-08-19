namespace AccountsService;

public record CreateAccountRequest(string CustomerName, decimal OpeningBalance);

public record AdjustBalanceRequest(decimal Delta);
