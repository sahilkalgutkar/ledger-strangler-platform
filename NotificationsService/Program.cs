using Microsoft.EntityFrameworkCore;
using NotificationsService.Consumers;
using NotificationsService.Data;
using NotificationsService.Services;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "notifications-service")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "/var/log/ledger-strangler-platform/notifications-service.log", rollingInterval: RollingInterval.Day));

var dbConnectionString = builder.Configuration.GetConnectionString("Notifications")
    ?? "Host=localhost;Port=5432;Database=notifications;Username=postgres;Password=postgres";
builder.Services.AddDbContext<NotificationsDbContext>(options => options.UseNpgsql(dbConnectionString));
builder.Services.AddScoped<NotificationService>();

var rabbitConnectionString = builder.Configuration["RabbitMq:ConnectionString"] ?? "amqp://guest:guest@localhost:5672";
builder.Services.AddSingleton(new BalanceChangedConsumerOptions(rabbitConnectionString));
builder.Services.AddHostedService<BalanceChangedConsumer>();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.EnsureCreated();
}

app.MapGet("/", () => "notifications-service: reacts to account balance events");

app.MapGet("/notifications", async (NotificationService svc) => Results.Ok(await svc.GetAllAsync()));

app.MapGet("/notifications/{accountId:guid}", async (Guid accountId, NotificationService svc) =>
    Results.Ok(await svc.GetForAccountAsync(accountId)));

app.Run();

public partial class Program
{
}
