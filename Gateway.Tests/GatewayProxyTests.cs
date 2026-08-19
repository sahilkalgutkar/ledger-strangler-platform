using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests;

public class GatewayProxyTests : IAsyncLifetime
{
    private FakeDownstream _accounts = null!;
    private FakeDownstream _legacy = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _accounts = await FakeDownstream.StartAsync("accounts");
        _legacy = await FakeDownstream.StartAsync("legacy");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Services:AccountsService", _accounts.Url);
            builder.UseSetting("Services:LegacyMonolith", _legacy.Url);
        });
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _accounts.DisposeAsync();
        await _legacy.DisposeAsync();
    }

    [Fact]
    public async Task Root_path_falls_through_to_the_legacy_monolith()
    {
        var body = await _client.GetStringAsync("/");
        Assert.StartsWith("handled-by:legacy:", body);
    }

    [Fact]
    public async Task Accounts_collection_is_routed_to_the_new_accounts_service()
    {
        var body = await _client.GetStringAsync("/accounts");
        Assert.StartsWith("handled-by:accounts:", body);
    }

    [Fact]
    public async Task Single_account_is_routed_to_the_new_accounts_service()
    {
        var body = await _client.GetStringAsync("/accounts/11111111-1111-1111-1111-111111111111");
        Assert.StartsWith("handled-by:accounts:", body);
    }

    [Fact]
    public async Task Balance_adjustments_are_routed_to_the_new_accounts_service()
    {
        var response = await _client.PostAsync(
            "/accounts/11111111-1111-1111-1111-111111111111/balance-adjustments", content: null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("handled-by:accounts:", body);
    }

    [Fact]
    public async Task Statements_still_fall_through_to_the_legacy_monolith()
    {
        var body = await _client.GetStringAsync("/accounts/11111111-1111-1111-1111-111111111111/statements");
        Assert.StartsWith("handled-by:legacy:", body);
    }
}
