using Gateway;
using System.Linq;
using Xunit;

namespace Gateway.Tests;

public class StranglerRoutesTests
{
    [Theory]
    [InlineData("/accounts", StranglerRoutes.AccountsCluster)]
    [InlineData("/accounts/", StranglerRoutes.AccountsCluster)]
    [InlineData("/accounts/11111111-1111-1111-1111-111111111111", StranglerRoutes.AccountsCluster)]
    [InlineData("/accounts/11111111-1111-1111-1111-111111111111/balance-adjustments", StranglerRoutes.AccountsCluster)]
    [InlineData("/accounts/11111111-1111-1111-1111-111111111111/statements", StranglerRoutes.LegacyCluster)]
    [InlineData("/accounts/11111111-1111-1111-1111-111111111111/statements/", StranglerRoutes.LegacyCluster)]
    [InlineData("/", StranglerRoutes.LegacyCluster)]
    [InlineData("/health", StranglerRoutes.LegacyCluster)]
    public void ResolveCluster_routes_each_path_to_the_right_side_of_the_migration(string path, string expectedCluster)
    {
        Assert.Equal(expectedCluster, StranglerRoutes.ResolveCluster(path));
    }

    [Fact]
    public void BuildConfig_orders_specific_routes_ahead_of_the_catchall()
    {
        var (routes, _) = StranglerRoutes.BuildConfig("http://accounts", "http://legacy");

        var catchall = routes.Single(r => r.RouteId == "legacy-catchall");
        foreach (var route in routes.Where(r => r.RouteId != "legacy-catchall"))
        {
            Assert.True(route.Order < catchall.Order, $"{route.RouteId} should be ordered ahead of the catch-all");
        }
    }

    [Fact]
    public void BuildConfig_points_each_cluster_at_its_configured_address()
    {
        var (_, clusters) = StranglerRoutes.BuildConfig("http://accounts:1", "http://legacy:2");

        var accounts = clusters.Single(c => c.ClusterId == StranglerRoutes.AccountsCluster);
        var legacy = clusters.Single(c => c.ClusterId == StranglerRoutes.LegacyCluster);

        Assert.Equal("http://accounts:1", accounts.Destinations!.Values.Single().Address);
        Assert.Equal("http://legacy:2", legacy.Destinations!.Values.Single().Address);
    }
}
