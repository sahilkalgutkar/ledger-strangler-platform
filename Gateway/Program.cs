using Gateway;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "gateway")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "/var/log/ledger-strangler-platform/gateway.log", rollingInterval: RollingInterval.Day));

var accountsServiceUrl = builder.Configuration["Services:AccountsService"] ?? "http://localhost:5301";
var legacyMonolithUrl = builder.Configuration["Services:LegacyMonolith"] ?? "http://localhost:5299";

var (routes, clusters) = StranglerRoutes.BuildConfig(accountsServiceUrl, legacyMonolithUrl);

builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapReverseProxy();

app.Run();

public partial class Program
{
}
