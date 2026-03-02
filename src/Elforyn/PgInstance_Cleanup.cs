namespace Elforyn;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    public Task Cleanup() =>
        Wrapper.DeleteInstance();
}
