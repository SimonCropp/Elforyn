namespace Elforyn;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    public async Task<PgDatabase<TDbContext>> BuildFromExisting(string dbName)
    {
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.OpenExistingDatabase(dbName);
        var database = new PgDatabase<TDbContext>(
            this,
            connection,
            dbName,
            constructInstance,
            () => Task.CompletedTask,
            null,
            npgsqlOptionsBuilder);
        await database.Start();
        return database;
    }
}
