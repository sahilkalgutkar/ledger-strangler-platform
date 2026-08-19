using AccountsService;
using AccountsService.Data;
using AccountsService.Events;
using AccountsService.Services;
using CassandraSession = Cassandra.ISession;
using Cassandra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    var contactPoint = builder.Configuration["Cassandra:ContactPoint"] ?? "localhost";
    var port = int.Parse(builder.Configuration["Cassandra:Port"] ?? "9042");
    var keyspace = builder.Configuration["Cassandra:Keyspace"] ?? "ledger";

    builder.Services.AddSingleton(_ => CassandraSessionFactory.Connect(contactPoint, port, keyspace));
    builder.Services.AddSingleton(sp => AccountsRepository.Create(sp.GetRequiredService<CassandraSession>()));
}

builder.Services.AddScoped<AccountService>();

var app = builder.Build();

app.MapGet("/", () => "accounts-service: strangled off the legacy monolith");

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
    catch (ConcurrentUpdateException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

public partial class Program
{
}
