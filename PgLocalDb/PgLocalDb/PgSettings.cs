namespace PgLocalDb;

public static class PgSettings
{
    static string? host;
    static int? port;
    static string? username;
    static string? password;

    public static string Host
    {
        get => host ?? "localhost";
        set => host = value;
    }

    public static int Port
    {
        get => port ?? 5432;
        set => port = value;
    }

    public static string Username
    {
        get => username ?? "postgres";
        set => username = value;
    }

    public static string Password
    {
        get => password ?? "postgres";
        set => password = value;
    }

    internal static string BuildConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            Database = database,
            Pooling = false
        };

        return builder.ToString();
    }
}
