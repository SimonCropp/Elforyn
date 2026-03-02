// ReSharper disable UnusedVariable

public class NpgsqlOptionsBuilder
{
    NpgsqlOptionsBuilder()
    {
        #region npgsqlOptionsBuilder

        var pgInstance = new PgInstance<MyDbContext>(
            "Host=localhost;Username=postgres;Password=postgres",
            constructInstance: builder => new(builder.Options),
            npgsqlOptionsBuilder: npgsqlBuilder => npgsqlBuilder.EnableRetryOnFailure(5));

        #endregion
    }

    class MyDbContext(DbContextOptions options) :
        DbContext(options);
}
