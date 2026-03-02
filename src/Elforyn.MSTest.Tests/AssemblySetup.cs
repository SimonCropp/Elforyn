[TestClass]
public class AssemblySetup
{
    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        await PgTestBase<TheDbContext>.Shutdown();
        await PgTestBase<DefaultTimestampDbContext>.Shutdown();
        await PgTestBase<TimestampDbContext>.Shutdown();
    }
}
