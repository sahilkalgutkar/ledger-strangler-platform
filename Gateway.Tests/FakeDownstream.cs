using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gateway.Tests;

/// <summary>
/// A tiny real HTTP server standing in for AccountsService or LegacyMonolith in the
/// proxy integration test, so the test proves YARP actually forwards bytes over the
/// network to the right place - not just that it picked the right route name.
/// </summary>
public sealed class FakeDownstream : IAsyncDisposable
{
    private readonly WebApplication _app;
    public string Url { get; }

    private FakeDownstream(WebApplication app, string url)
    {
        _app = app;
        Url = url;
    }

    public static async Task<FakeDownstream> StartAsync(string label)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        app.MapGet("/{**catchall}", (HttpContext ctx) => Results.Text($"handled-by:{label}:{ctx.Request.Path}"));
        app.MapPost("/{**catchall}", (HttpContext ctx) => Results.Text($"handled-by:{label}:{ctx.Request.Path}"));

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        return new FakeDownstream(app, address);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
