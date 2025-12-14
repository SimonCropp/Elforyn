namespace EfPgLocalDb;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    internal PgLocalDb.Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage DefaultStorage;
    Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder;

    static PgInstance()
    {
        var type = typeof(TDbContext);
        var name = type.Name;
        if (type.IsNested)
        {
            name = $"{type.DeclaringType!.Name}_{name}";
        }
        DefaultStorage = new(name, PgLocalDb.DirectoryFinder.Find(name));
    }

    public IModel Model { get; } = null!;

    /// <summary>
    ///     Instantiate a <see cref="PgInstance{TDbContext}" />.
    ///     Should usually be scoped as one instance per appdomain. So all tests use the same instance of
    ///     <see cref="PgInstance{TDbContext}" />.
    /// </summary>
    /// <param name="constructInstance"></param>
    /// <param name="buildTemplate"></param>
    /// <param name="storage">Disk storage convention for where the timestamp files will be located.</param>
    /// <param name="timestamp"></param>
    /// <param name="callback">Option callback that is executed after the template database has been created.</param>
    /// <param name="npgsqlOptionsBuilder">Passed to <see cref="NpgsqlDbContextOptionsExtensions.UseNpgsql(DbContextOptionsBuilder,string,Action{NpgsqlDbContextOptionsBuilder})" />.</param>
    public PgInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate = null,
        Storage? storage = null,
        DateTime? timestamp = null,
        Callback<TDbContext>? callback = null,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder = null) :
        this(
            constructInstance,
            BuildTemplateConverter.Convert(constructInstance, buildTemplate),
            storage,
            GetTimestamp(timestamp, buildTemplate),
            callback,
            npgsqlOptionsBuilder)
    {
    }

    /// <summary>
    ///     Instantiate a <see cref="PgInstance{TDbContext}" />.
    ///     Should usually be scoped as one instance per appdomain. So all tests use the same instance of
    ///     <see cref="PgInstance{TDbContext}" />.
    /// </summary>
    /// <param name="constructInstance"></param>
    /// <param name="buildTemplate"></param>
    /// <param name="storage">Disk storage convention for where the timestamp files will be located. Optional.</param>
    /// <param name="timestamp"></param>
    /// <param name="callback">Callback that is executed after the template database has been created. Optional.</param>
    /// <param name="npgsqlOptionsBuilder">Passed to <see cref="NpgsqlDbContextOptionsExtensions.UseNpgsql(DbContextOptionsBuilder,string,Action{NpgsqlDbContextOptionsBuilder})" />.</param>
    public PgInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromConnection<TDbContext> buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        Callback<TDbContext>? callback = null,
        Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder = null)
    {
        storage ??= DefaultStorage;
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        Model = BuildModel(constructInstance, npgsqlOptionsBuilder);
        this.constructInstance = constructInstance;
        this.npgsqlOptionsBuilder = npgsqlOptionsBuilder;

        var storageValue = storage.Value;
        StorageDirectory = storageValue.Directory;
        PgLocalDb.DirectoryCleaner.CleanInstance(StorageDirectory);

        Task BuildTemplate(DbConnection connection)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseNpgsql(connection, npgsqlOptionsBuilder);
            return buildTemplate(connection, builder);
        }

        Func<DbConnection, Task>? wrapperCallback = null;
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
            _ => new NpgsqlConnection(_),
            storageValue.Name,
            StorageDirectory,
            wrapperCallback);

        Wrapper.Start(resultTimestamp, BuildTemplate);
    }

    public string StorageDirectory { get; } = null!;

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is null)
        {
            return PgLocalDb.Timestamp.LastModified<TDbContext>();
        }

        return PgLocalDb.Timestamp.LastModified(buildTemplate);
    }

    static IModel BuildModel(ConstructInstance<TDbContext> constructInstance, Action<NpgsqlDbContextOptionsBuilder>? npgsqlOptionsBuilder)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseNpgsql("Host=fake", npgsqlOptionsBuilder);
        return constructInstance(builder).Model;
    }

    Task<string> BuildDatabase(string dbName) => Wrapper.CreateDatabaseFromTemplate(dbName);

    public string MasterConnectionString => Wrapper.MasterConnectionString;
}
