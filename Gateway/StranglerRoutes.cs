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

    /// <summary>
    /// Statements haven't been strangled yet - they still read the legacy monolith's
    /// own copy of account data, so they stay routed to it even though account
    /// CRUD/balance management has already moved to the new service.
    ///
    /// Originally this enumerated each individual accounts-service path
    /// (collection, single account, balance-adjustments) as its own exact match.
    /// A request with a malformed/empty id segment - "/accounts//balance-
    /// adjustments" - didn't match any of them and fell through to the legacy
    /// catch-all instead, which has no route for it either. Modeling it instead
    /// as "everything under /accounts belongs to the new service, except this
    /// one carved-out exception" is both simpler and doesn't have that gap: any
    /// path starting with /accounts reaches a service that can actually reject
    /// it properly, rather than silently landing on the wrong side of the cut.
    /// </summary>
    public static string ResolveCluster(string path)
    {
        if (StatementsPath.IsMatch(path))
        {
            return LegacyCluster;
        }

        if (path.StartsWith("/accounts", StringComparison.OrdinalIgnoreCase))
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
                RouteId = "accounts-catchall",
                Order = 2,
                ClusterId = AccountsCluster,
                Match = new RouteMatch { Path = "/accounts/{**catchall}" }
            },
            new RouteConfig
            {
                RouteId = "accounts-collection",
                Order = 3,
                ClusterId = AccountsCluster,
                Match = new RouteMatch { Path = "/accounts" }
            },
            new RouteConfig
            {
                RouteId = "legacy-catchall",
                Order = 4,
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
