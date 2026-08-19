namespace LegacyMonolith;

public record CreateAccountRequest(string CustomerName, decimal OpeningBalance);

public record AdjustBalanceRequest(decimal Delta);

public record GenerateStatementRequest(DateTime PeriodStart, DateTime PeriodEnd);
