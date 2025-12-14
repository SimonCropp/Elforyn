namespace EfPgLocalDb;

public partial class PgInstance<TDbContext>
{
    /// <summary>
    ///     Build database with a name based on the calling Method.
    /// </summary>
    /// <param name="data">
    ///     Data to be added to the database when it is created.
    /// </param>
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
    ///     Will default to the caller method name.
    /// </param>
    public Task<PgDatabase<TDbContext>> Build(
        IEnumerable<object>? data = null,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        var testClass = Path.GetFileNameWithoutExtension(testFile);
        var name = PgLocalDb.DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
        return Build(name, data);
    }

    /// <summary>
    ///     Build database with an explicit name.
    /// </summary>
    public async Task<PgDatabase<TDbContext>> Build(string dbName, IEnumerable<object>? data = null)
    {
        var connection = await BuildDatabase(dbName);
        var database = new PgDatabase<TDbContext>(
            this,
            connection,
            dbName,
            constructInstance,
            () => Wrapper.DeleteDatabase(dbName),
            data,
            npgsqlOptionsBuilder);
        await database.Start();
        return database;
    }

    public async Task Cleanup()
    {
        await Wrapper.Cleanup();
    }
}
