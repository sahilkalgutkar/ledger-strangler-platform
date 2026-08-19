using CassandraSession = Cassandra.ISession;
using Cassandra;

namespace AccountsService.Data;

public static class CassandraSessionFactory
{
    public static CassandraSession Connect(string contactPoint, int port, string keyspace)
    {
        var cluster = Cluster.Builder()
            .AddContactPoint(contactPoint)
            .WithPort(port)
            .Build();

        using (var bootstrap = cluster.Connect())
        {
            bootstrap.Execute(
                $"CREATE KEYSPACE IF NOT EXISTS {keyspace} " +
                "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}");
        }

        var session = cluster.Connect(keyspace);
        session.Execute(@"CREATE TABLE IF NOT EXISTS accounts (
            id uuid PRIMARY KEY,
            customer_name text,
            balance decimal,
            created_at timestamp
        )");

        return session;
    }
}
