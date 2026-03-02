// ReSharper disable RedundantCast
namespace Elforyn;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    public Task<PgDatabase<TDbContext>> Build(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        Ensure.NotNullOrWhiteSpace(testFile);
        Ensure.NotNullOrWhiteSpace(memberName);
        Ensure.NotWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
        return Build(dbName, data);
    }

    public async Task<TDbContext> BuildContext(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        await using var build = await Build(data, testFile, databaseSuffix, memberName);
        return build.NewConnectionOwnedDbContext();
    }

    public Task<PgDatabase<TDbContext>> Build(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        Build(null, testFile, databaseSuffix, memberName);

    public async Task<TDbContext> BuildContext(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        await using var build = await Build(testFile, databaseSuffix, memberName);
        return build.NewConnectionOwnedDbContext();
    }

    public async Task<PgDatabase<TDbContext>> Build(
        string dbName,
        IEnumerable<object>? data)
    {
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.CreateDatabaseFromTemplate(dbName);
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

    public async Task<TDbContext> BuildContext(
        string dbName,
        IEnumerable<object>? data)
    {
        await using var build = await Build(dbName, data);
        return build.NewConnectionOwnedDbContext();
    }

    public Task<PgDatabase<TDbContext>> Build(string dbName) =>
        Build(dbName, (IEnumerable<object>?) null);

    public async Task<TDbContext> BuildContext(string dbName)
    {
        await using var build = await Build(dbName, (IEnumerable<object>?) null);
        return build.NewConnectionOwnedDbContext();
    }
}
