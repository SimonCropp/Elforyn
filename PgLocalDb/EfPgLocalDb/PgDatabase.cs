namespace EfPgLocalDb;

public partial class PgDatabase<TDbContext> :
    IAsyncDisposable,
    IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    PgInstance<TDbContext> instance;
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    IEnumerable<object>? data;
    Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder;

    internal PgDatabase(
        PgInstance<TDbContext> instance,
        string connectionString,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder)
    {
        Name = name;
        this.instance = instance;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        this.npgsqlOptionsBuilder = npgsqlOptionsBuilder;
        ConnectionString = connectionString;
        Connection = new(connectionString);
    }

    public string Name { get; }
    public NpgsqlConnection Connection { get; }
    public string ConnectionString { get; }

    public async Task<NpgsqlConnection> OpenNewConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static implicit operator TDbContext(PgDatabase<TDbContext> instance) => instance.Context;

    public static implicit operator NpgsqlConnection(PgDatabase<TDbContext> instance) => instance.Connection;

    public static implicit operator DbConnection(PgDatabase<TDbContext> instance) => instance.Connection;

    public async Task Start()
    {
        await Connection.OpenAsync();

        Context = NewDbContext();
        NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);

        if (data is not null)
        {
            await AddData(data);
        }
    }

    public TDbContext Context { get; private set; } = null!;
    public TDbContext NoTrackingContext { get; private set; } = null!;

    TDbContext IDbContextFactory<TDbContext>.CreateDbContext() => NewConnectionOwnedDbContext();

    public TDbContext NewDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql(Connection, npgsqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

    public TDbContext NewConnectionOwnedDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql(Connection.ConnectionString, npgsqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }

        if (NoTrackingContext is not null)
        {
            await NoTrackingContext.DisposeAsync();
        }

        await Connection.DisposeAsync();
    }

    public async Task Delete()
    {
        await DisposeAsync();
        await delete();
    }

    /// <summary>
    ///     Returns <see cref="DbContext.Set{TEntity}()" /> from <see cref="NoTrackingContext" />.
    /// </summary>
    public DbSet<T> Set<T>()
        where T : class => NoTrackingContext.Set<T>();

    async Task AddData(IEnumerable<object> entities)
    {
        Context.AddRange(entities);
        await Context.SaveChangesAsync();
    }
}
