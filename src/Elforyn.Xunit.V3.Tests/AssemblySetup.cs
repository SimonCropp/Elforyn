[assembly: AssemblyFixture(typeof(AssemblySetup))]

public class AssemblySetup : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        PgTestBase<TheDbContext>.Shutdown();
        PgTestBase<DefaultTimestampDbContext>.Shutdown();
        PgTestBase<TimestampDbContext>.Shutdown();
        return ValueTask.CompletedTask;
    }
}
