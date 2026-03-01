namespace Elforyn;

public partial class PgInstance<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    internal Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage defaultStorage;
    Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder;

    static PgInstance()
    {
        var type = typeof(TDbContext);
        var name = type.Name;
        if (type.IsNested)
        {
            name = $"{type.DeclaringType!.Name}_{name}";
        }

        defaultStorage = new(name);
    }

    public IModel Model { get; } = null!;

    public string ConnectionString { get; } = null!;

    public PgInstance(
        string connectionString,
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate = null,
        Storage? storage = null,
        DateTime? timestamp = null,
        Callback<TDbContext>? callback = null,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder = null) :
        this(
            connectionString,
            constructInstance,
            BuildTemplateConverter.Convert(constructInstance, buildTemplate),
            storage,
            GetTimestamp(timestamp, buildTemplate),
            callback,
            npgsqlOptionsBuilder)
    {
    }

    public PgInstance(
        string connectionString,
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromConnection<TDbContext> buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        Callback<TDbContext>? callback = null,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder = null)
    {
        storage ??= defaultStorage;
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        Model = BuildModel(constructInstance, connectionString, npgsqlOptionsBuilder);
        InitEntityMapping();
        this.constructInstance = constructInstance;
        this.npgsqlOptionsBuilder = npgsqlOptionsBuilder;
        ConnectionString = connectionString;

        var storageValue = storage.Value;

        Task BuildTemplate(NpgsqlConnection connection)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseNpgsql(connection, npgsqlOptionsBuilder);
            return buildTemplate(connection, builder);
        }

        Func<NpgsqlConnection, Task>? wrapperCallback = null;
        if (callback is not null)
        {
            wrapperCallback = async connection =>
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseNpgsql(connection, npgsqlOptionsBuilder);
                await using var context = constructInstance(builder);
                await callback(connection, context);
            };
        }

        Wrapper = new(
            connectionString,
            storageValue.Name,
            wrapperCallback);

        Wrapper.Start(resultTimestamp, BuildTemplate);
    }

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is null)
        {
            return Timestamp.LastModified<TDbContext>();
        }

        return Timestamp.LastModified(buildTemplate);
    }

    static IModel BuildModel(ConstructInstance<TDbContext> constructInstance, string connectionString, Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql(connectionString, npgsqlOptionsBuilder);
        using var context = constructInstance(builder);
        return context.Model;
    }

    public void Dispose() =>
        Wrapper.Dispose();
}
