using Gateway;

var builder = WebApplication.CreateBuilder(args);

var accountsServiceUrl = builder.Configuration["Services:AccountsService"] ?? "http://localhost:5301";
var legacyMonolithUrl = builder.Configuration["Services:LegacyMonolith"] ?? "http://localhost:5299";

var (routes, clusters) = StranglerRoutes.BuildConfig(accountsServiceUrl, legacyMonolithUrl);

builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);

var app = builder.Build();

app.MapReverseProxy();

app.Run();

public partial class Program
{
}
