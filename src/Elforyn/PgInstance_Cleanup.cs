namespace Elforyn;

public partial class PgInstance<TDbContext>
    where TDbContext : DbContext
{
    public async Task Cleanup()
    {
        await Wrapper.DeleteInstance();
    }
}
