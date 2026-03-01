namespace Elforyn;

public static class ElforynSettings
{
    internal static Action<NpgsqlConnectionStringBuilder>? connectionBuilder;

    public static void ConnectionBuilder(Action<NpgsqlConnectionStringBuilder> builder) =>
        connectionBuilder = builder;

    internal static string BuildConnectionString(string connectionString, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = database
        };
        connectionBuilder?.Invoke(builder);
        return builder.ConnectionString;
    }
}
