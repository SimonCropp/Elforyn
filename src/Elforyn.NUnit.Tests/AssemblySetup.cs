[SetUpFixture]
public class AssemblySetup
{
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        await PgTestBase<TheDbContext>.Shutdown();
        await PgTestBase<DefaultTimestampDbContext>.Shutdown();
        await PgTestBase<TimestampDbContext>.Shutdown();
    }
}
