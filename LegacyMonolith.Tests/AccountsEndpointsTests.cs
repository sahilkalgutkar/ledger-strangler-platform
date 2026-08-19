using System.Net;
using System.Net.Http.Json;
using LegacyMonolith.Models;
using Xunit;

namespace LegacyMonolith.Tests;

public class AccountsEndpointsTests : IClassFixture<LegacyApiFactory>
{
    private readonly HttpClient _client;

    public AccountsEndpointsTests(LegacyApiFactory factory)
    {
        _client = factory.CreateClient();
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
    public async Task GetAccount_returns_404_for_unknown_id()
    {
        var response = await _client.GetAsync($"/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    [Fact]
    public async Task GenerateStatement_returns_400_for_inverted_period()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("Jane Doe", 100m));
        var account = await created.Content.ReadFromJsonAsync<Account>();

        var response = await _client.PostAsJsonAsync(
            $"/accounts/{account!.Id}/statements",
            new GenerateStatementRequest(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateStatement_returns_404_for_unknown_account()
    {
        var response = await _client.PostAsJsonAsync(
            $"/accounts/{Guid.NewGuid()}/statements",
            new GenerateStatementRequest(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatements_returns_generated_statement()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("Jane Doe", 500m));
        var account = await created.Content.ReadFromJsonAsync<Account>();
        await _client.PostAsJsonAsync(
            $"/accounts/{account!.Id}/statements",
            new GenerateStatementRequest(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));

        var response = await _client.GetAsync($"/accounts/{account.Id}/statements");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statements = await response.Content.ReadFromJsonAsync<List<Statement>>();
        Assert.Single(statements!);
    }
}
