[TestClass]
public class AssemblySetup
{
    [AssemblyCleanup]
    public static void Cleanup()
    {
        PgTestBase<TheDbContext>.Shutdown();
        PgTestBase<DefaultTimestampDbContext>.Shutdown();
        PgTestBase<TimestampDbContext>.Shutdown();
    }
}
