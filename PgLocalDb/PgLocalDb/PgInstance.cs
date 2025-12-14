namespace PgLocalDb;

public class PgInstance
{
    internal readonly Wrapper Wrapper = null!;

    public PgInstance(
        string name,
        Func<DbConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        Func<DbConnection, Task>? callback = null,
        Func<string, DbConnection>? buildConnection = null)
    {
        Guard.AgainstWhiteSpace(directory);
        Guard.AgainstNullWhiteSpace(name);
        directory = DirectoryFinder.Find(name);
        DirectoryCleaner.CleanInstance(directory);
        var callingAssembly = Assembly.GetCallingAssembly();
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate, callingAssembly);
        buildConnection ??= _ => new NpgsqlConnection(_);

        Wrapper = new(buildConnection, name, directory, callback);
        Wrapper.Start(resultTimestamp, buildTemplate);
    }

    public PgInstance(
        string name,
        Func<NpgsqlConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        Func<NpgsqlConnection, Task>? callback = null) :
        this(
            name,
            connection => buildTemplate((NpgsqlConnection)connection),
            directory,
            timestamp,
            connection =>
            {
                if (callback == null)
                {
                    return Task.CompletedTask;
                }

                return callback.Invoke((NpgsqlConnection)connection);
            },
            _ => new NpgsqlConnection(_))
    {
    }

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate, Assembly callingAssembly)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is not null)
        {
            return Timestamp.LastModified(buildTemplate);
        }

        return Timestamp.LastModified(callingAssembly);
    }

    public async Task Cleanup()
    {
        await Wrapper.Cleanup();
    }

    Task<string> BuildContext(string dbName) => Wrapper.CreateDatabaseFromTemplate(dbName);

    /// <summary>
    ///     Build database with a name based on the calling Method.
    /// </summary>
    /// <param name="testFile">
    ///     The path to the test class.
    ///     Used to make the database name unique per test type.
    /// </param>
    /// <param name="databaseSuffix">
    ///     For Xunit theories add some text based on the inline data
    ///     to make the db name unique.
    /// </param>
    /// <param name="memberName">
    ///     Used to make the db name unique per method.
    ///     Will default to the caller method name is used.
    /// </param>
    public Task<PgDatabase> Build(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
    {
        Guard.AgainstNullWhiteSpace(testFile);
        Guard.AgainstNullWhiteSpace(memberName);
        Guard.AgainstWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var name = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);

        return Build(name);
    }

    /// <summary>
    ///     Build database with an explicit name.
    /// </summary>
    public async Task<PgDatabase> Build(string dbName)
    {
        Guard.AgainstNullWhiteSpace(dbName);
        var connection = await BuildContext(dbName);
        var database = new PgDatabase(connection, dbName, () => Wrapper.DeleteDatabase(dbName));
        await database.Start();
        return database;
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;
}
