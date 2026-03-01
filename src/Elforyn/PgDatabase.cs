namespace Elforyn;

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
    bool readOnly;

    internal PgDatabase(
        PgInstance<TDbContext> instance,
        NpgsqlConnection connection,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder,
        bool readOnly = false,
        NpgsqlTransaction? transaction = null)
    {
        Name = name;
        this.instance = instance;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        this.npgsqlOptionsBuilder = npgsqlOptionsBuilder;
        this.readOnly = readOnly;
        ConnectionString = connection.ConnectionString;
        Connection = connection;
        Transaction = transaction;
    }

    public NpgsqlTransaction? Transaction { get; }

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

    internal Task Start()
    {
        Context = NewDbContext();
        NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);

        if (Transaction is not null)
        {
            Context.Database.UseTransaction(Transaction);
            NoTrackingContext.Database.UseTransaction(Transaction);
        }

        if (data is not null)
        {
            return AddData(data);
        }

        return Task.CompletedTask;
    }

    public TDbContext Context { get; private set; } = null!;

    public TDbContext NoTrackingContext { get; private set; } = null!;

    TDbContext IDbContextFactory<TDbContext>.CreateDbContext() => NewConnectionOwnedDbContext();

    public TDbContext NewDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql(Connection, npgsqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        if (readOnly)
        {
            builder.AddInterceptors(ReadOnlyInterceptor.Instance);
        }

        return constructInstance(builder);
    }

    public TDbContext NewConnectionOwnedDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql(Connection.ConnectionString, npgsqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        if (readOnly)
        {
            builder.AddInterceptors(ReadOnlyInterceptor.Instance);
        }

        return constructInstance(builder);
    }

    public async ValueTask DisposeAsync()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (NoTrackingContext is not null)
        {
            await NoTrackingContext.DisposeAsync();
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        if (Transaction != null)
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }

        await Connection.DisposeAsync();
    }

    public async Task Delete()
    {
        await DisposeAsync();
        await delete();
    }

    public DbSet<T> Set<T>()
        where T : class => NoTrackingContext.Set<T>();

    IEnumerable<object> ExpandEnumerable(IEnumerable<object> entities) =>
        DbContextExtensions.ExpandEnumerable(entities, instance.EntityTypes);
}
