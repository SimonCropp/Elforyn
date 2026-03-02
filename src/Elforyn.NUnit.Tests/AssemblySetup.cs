[SetUpFixture]
public class AssemblySetup
{
    [OneTimeTearDown]
    public void Cleanup()
    {
        PgTestBase<TheDbContext>.Shutdown();
        PgTestBase<DefaultTimestampDbContext>.Shutdown();
        PgTestBase<TimestampDbContext>.Shutdown();
    }
}
