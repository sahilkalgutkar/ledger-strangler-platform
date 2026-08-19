using System.Net;
using System.Net.Http.Json;
using AccountsService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Cassandra;
using Testcontainers.RabbitMq;
using Xunit;

namespace AccountsService.Tests;

/// <summary>
/// Drives the real HTTP surface (not the service classes directly) against
/// real Cassandra and RabbitMQ via Testcontainers, so Program.cs's endpoint
/// mapping and status-code-per-exception branches are actually exercised.
/// </summary>
public class AccountsEndpointsTests : IAsyncLifetime
{
    private readonly CassandraContainer _cassandra = new CassandraBuilder().Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().Build();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_cassandra.StartAsync(), _rabbitMq.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Cassandra:ContactPoint", _cassandra.Hostname);
            builder.UseSetting("Cassandra:Port", _cassandra.GetMappedPublicPort(9042).ToString());
            builder.UseSetting("Cassandra:Keyspace", "ledger_endpoints_test");
            builder.UseSetting("RabbitMq:ConnectionString", _rabbitMq.GetConnectionString());
        });
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _cassandra.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task PostAccounts_creates_and_returns_201()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("Jane Doe", 100m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<Account>();
        Assert.Equal("Jane Doe", account!.CustomerName);
    }

    [Fact]
    public async Task GetAccounts_lists_created_accounts()
    {
        await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("List Me", 10m));

        var accounts = await _client.GetFromJsonAsync<List<Account>>("/accounts");

        Assert.Contains(accounts!, a => a.CustomerName == "List Me");
    }

    [Fact]
    public async Task GetAccount_returns_404_for_unknown_id()
    {
        var response = await _client.GetAsync($"/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BalanceAdjustment_returns_200_and_updated_balance()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("Jane Doe", 100m));
        var account = await created.Content.ReadFromJsonAsync<Account>();

        var response = await _client.PostAsJsonAsync(
            $"/accounts/{account!.Id}/balance-adjustments", new AdjustBalanceRequest(50m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Account>();
        Assert.Equal(150m, updated!.Balance);
    }

    [Fact]
    public async Task BalanceAdjustment_returns_409_when_it_would_go_negative()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("Low Balance", 10m));
        var account = await created.Content.ReadFromJsonAsync<Account>();

        var response = await _client.PostAsJsonAsync(
            $"/accounts/{account!.Id}/balance-adjustments", new AdjustBalanceRequest(-50m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task BalanceAdjustment_returns_404_for_unknown_account()
    {
        var response = await _client.PostAsJsonAsync(
            $"/accounts/{Guid.NewGuid()}/balance-adjustments", new AdjustBalanceRequest(10m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
