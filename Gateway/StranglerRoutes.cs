using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;

namespace Gateway;

/// <summary>
/// The strangler fig cut line: which requests have already been migrated off the
/// legacy monolith, and which still fall through to it. This is deliberately the
/// only place that decision gets made, so as more of the monolith gets strangled,
/// this is the only file that changes.
/// </summary>
public static class StranglerRoutes
{
    public const string AccountsCluster = "accounts-service";
    public const string LegacyCluster = "legacy-monolith";

    private static readonly Regex StatementsPath = new(@"^/accounts/[^/]+/statements/?$", RegexOptions.Compiled);
    private static readonly Regex BalanceAdjustmentsPath = new(@"^/accounts/[^/]+/balance-adjustments/?$", RegexOptions.Compiled);
    private static readonly Regex SingleAccountPath = new(@"^/accounts/[^/]+/?$", RegexOptions.Compiled);
    private static readonly Regex AccountsCollectionPath = new(@"^/accounts/?$", RegexOptions.Compiled);

    /// <summary>
    /// Statements haven't been strangled yet - they still read the legacy monolith's
    /// own copy of account data, so they stay routed to it even though account
    /// CRUD/balance management has already moved to the new service.
    /// </summary>
    public static string ResolveCluster(string path)
    {
        if (StatementsPath.IsMatch(path))
        {
            return LegacyCluster;
        }

        if (BalanceAdjustmentsPath.IsMatch(path) || SingleAccountPath.IsMatch(path) || AccountsCollectionPath.IsMatch(path))
        {
            return AccountsCluster;
        }

        return LegacyCluster;
    }

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) BuildConfig(
        string accountsServiceUrl, string legacyMonolithUrl)
    {
        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "statements",
                Order = 1,
                ClusterId = LegacyCluster,
                Match = new RouteMatch { Path = "/accounts/{id}/statements" }
            },
            new RouteConfig
            {
                RouteId = "balance-adjustments",
                Order = 2,
                ClusterId = AccountsCluster,
                Match = new RouteMatch { Path = "/accounts/{id}/balance-adjustments" }
            },
            new RouteConfig
            {
                RouteId = "single-account",
                Order = 3,
                ClusterId = AccountsCluster,
                Match = new RouteMatch { Path = "/accounts/{id}" }
            },
            new RouteConfig
            {
                RouteId = "accounts-collection",
                Order = 4,
                ClusterId = AccountsCluster,
                Match = new RouteMatch { Path = "/accounts" }
            },
            new RouteConfig
            {
                RouteId = "legacy-catchall",
                Order = 5,
                ClusterId = LegacyCluster,
                Match = new RouteMatch { Path = "/{**catchall}" }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = AccountsCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["accounts-primary"] = new DestinationConfig { Address = accountsServiceUrl }
                }
            },
            new ClusterConfig
            {
                ClusterId = LegacyCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["legacy-primary"] = new DestinationConfig { Address = legacyMonolithUrl }
                }
            }
        };

        return (routes, clusters);
    }
}
