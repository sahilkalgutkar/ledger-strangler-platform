using AccountsService;
using AccountsService.Data;
using AccountsService.Events;
using AccountsService.Services;
using CassandraSession = Cassandra.ISession;
using Cassandra;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "accounts-service")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "/var/log/ledger-strangler-platform/accounts-service.log", rollingInterval: RollingInterval.Day));

if (!builder.Environment.IsEnvironment("Testing"))
{
    var contactPoint = builder.Configuration["Cassandra:ContactPoint"] ?? "localhost";
    var port = int.Parse(builder.Configuration["Cassandra:Port"] ?? "9042");
    var keyspace = builder.Configuration["Cassandra:Keyspace"] ?? "ledger";

    builder.Services.AddSingleton(_ => CassandraSessionFactory.Connect(contactPoint, port, keyspace));
    builder.Services.AddSingleton(sp => AccountsRepository.Create(sp.GetRequiredService<CassandraSession>()));

    var rabbitConnectionString = builder.Configuration["RabbitMq:ConnectionString"] ?? "amqp://guest:guest@localhost:5672";
    builder.Services.AddSingleton<IEventPublisher>(_ => new RabbitMqEventPublisher(rabbitConnectionString));
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
}

builder.Services.AddScoped<AccountService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

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
