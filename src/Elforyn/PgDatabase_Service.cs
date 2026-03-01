namespace Elforyn;

public partial class PgDatabase<TDbContext> :
    IServiceScopeFactory,
    IServiceProvider
{
    public object? GetService(Type type)
    {
        if (type == typeof(NpgsqlConnection))
        {
            return Connection;
        }

        if (type == typeof(TDbContext))
        {
            return Context;
        }

        if (type == typeof(IServiceScopeFactory))
        {
            return this;
        }

        return null;
    }

    public IServiceScope CreateScope()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(NewDbContext(), connection);
    }

    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
}
