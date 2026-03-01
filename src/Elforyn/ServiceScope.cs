class ServiceScope(DbContext context, NpgsqlConnection connection) :
    IServiceScope,
    IServiceProvider,
    IAsyncDisposable
{
    public void Dispose()
    {
        connection.Dispose();
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        await context.DisposeAsync();
    }

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type type)
    {
        if (type == typeof(NpgsqlConnection))
        {
            return connection;
        }

        if (type == context.GetType())
        {
            return context;
        }

        return null;
    }
}
