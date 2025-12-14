namespace EfPgLocalDb;

static class QueryTrackingExtensions
{
    public static void ApplyQueryTracking<TDbContext>(
        this DbContextOptionsBuilder<TDbContext> builder,
        QueryTrackingBehavior? tracking)
        where TDbContext : DbContext
    {
        if (tracking.HasValue)
        {
            builder.UseQueryTrackingBehavior(tracking.Value);
        }
    }
}
