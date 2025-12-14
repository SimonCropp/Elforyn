namespace PgLocalDb;

public partial class PgDatabase :
#if NET5_0_OR_GREATER
    IAsyncDisposable,
#endif
    IDisposable
{
    Func<Task> delete;

    internal PgDatabase(string connectionString, string name, Func<Task> delete)
    {
        this.delete = delete;
        ConnectionString = connectionString;
        Name = name;
        Connection = new(connectionString);
    }

    public string ConnectionString { get; }
    public string Name { get; }

    public NpgsqlConnection Connection { get; }

    public static implicit operator NpgsqlConnection(PgDatabase instance) => instance.Connection;

    public static implicit operator DbConnection(PgDatabase instance) => instance.Connection;

    public async Task<NpgsqlConnection> OpenNewConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public Task Start() => Connection.OpenAsync();

    public void Dispose() =>
        Connection.Dispose();

#if NET5_0_OR_GREATER
    public ValueTask DisposeAsync() =>
        Connection.DisposeAsync();
#endif

    public async Task Delete()
    {
#if NET48
        Dispose();
#else
        await DisposeAsync();
#endif
        await delete();
    }
}
