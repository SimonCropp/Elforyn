[assembly: AssemblyFixture(typeof(AssemblySetup))]

public class AssemblySetup : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await PgTestBase<TheDbContext>.Shutdown();
        await PgTestBase<DefaultTimestampDbContext>.Shutdown();
        await PgTestBase<TimestampDbContext>.Shutdown();
    }
}
