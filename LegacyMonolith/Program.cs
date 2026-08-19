using LegacyMonolith;
using LegacyMonolith.Data;
using LegacyMonolith.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Legacy")
    ?? "Host=localhost;Port=5432;Database=legacy_monolith;Username=postgres;Password=postgres";

builder.Services.AddDbContext<LegacyDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<StatementService>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<LegacyDbContext>().Database.EnsureCreated();
}

app.MapGet("/", () => "legacy-monolith: accounts + statements, all in one process");

app.MapPost("/accounts", async (CreateAccountRequest req, AccountService svc) =>
{
    var account = await svc.CreateAccountAsync(req.CustomerName, req.OpeningBalance);
    return Results.Created($"/accounts/{account.Id}", account);
});

app.MapGet("/accounts", async (AccountService svc) => Results.Ok(await svc.GetAllAccountsAsync()));

app.MapGet("/accounts/{id:guid}", async (Guid id, AccountService svc) =>
{
    var account = await svc.GetAccountAsync(id);
    return account is null ? Results.NotFound() : Results.Ok(account);
});

app.MapPost("/accounts/{id:guid}/balance-adjustments", async (Guid id, AdjustBalanceRequest req, AccountService svc) =>
{
    try
    {
        var account = await svc.AdjustBalanceAsync(id, req.Delta);
        return Results.Ok(account);
    }
    catch (AccountNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InsufficientBalanceException ex)
    {
        return Results.Conflict(ex.Message);
    }
});

app.MapPost("/accounts/{id:guid}/statements", async (Guid id, GenerateStatementRequest req, StatementService svc) =>
{
    try
    {
        var statement = await svc.GenerateStatementAsync(id, req.PeriodStart, req.PeriodEnd);
        return Results.Created($"/accounts/{id}/statements", statement);
    }
    catch (AccountNotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/accounts/{id:guid}/statements", async (Guid id, StatementService svc) =>
    Results.Ok(await svc.GetStatementsAsync(id)));

app.Run();

public partial class Program
{
}
