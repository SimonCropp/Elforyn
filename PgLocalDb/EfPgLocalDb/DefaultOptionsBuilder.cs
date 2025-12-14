namespace EfPgLocalDb;

static class DefaultOptionsBuilder
{
    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.EnableDetailedErrors();
        builder.EnableSensitiveDataLogging();
        return builder;
    }
}
