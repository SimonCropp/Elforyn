namespace Elforyn;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    public async Task<PgDatabase<TDbContext>> BuildShared(bool useTransaction = false)
    {
        var connection = await Wrapper.OpenSharedDatabase();

        NpgsqlTransaction? transaction = null;
        if (useTransaction)
        {
            transaction = await connection.BeginTransactionAsync();
        }

        var database = new PgDatabase<TDbContext>(
            this,
            connection,
            "Shared",
            constructInstance,
            () => Task.CompletedTask,
            null,
            npgsqlOptionsBuilder,
            readOnly: !useTransaction,
            transaction: transaction);
        await database.Start();
        return database;
    }

    public async Task<PgDatabase<TDbContext>> BuildShared(
        IEnumerable<object>? data,
        bool useTransaction = false)
    {
        Func<NpgsqlConnection, Task>? initialize = null;
        if (data != null)
        {
            initialize = async initConnection =>
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseNpgsql(initConnection, npgsqlOptionsBuilder);
                await using var context = constructInstance(builder);
                await context.AddData(data, EntityTypes);
            };
        }

        var connection = await Wrapper.OpenSharedDatabase(initialize);

        NpgsqlTransaction? transaction = null;
        if (useTransaction)
        {
            transaction = await connection.BeginTransactionAsync();
        }

        var database = new PgDatabase<TDbContext>(
            this,
            connection,
            "Shared",
            constructInstance,
            () => Task.CompletedTask,
            null,
            npgsqlOptionsBuilder,
            readOnly: !useTransaction,
            transaction: transaction);
        await database.Start();
        return database;
    }
}
