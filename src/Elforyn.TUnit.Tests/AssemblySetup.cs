public static class AssemblySetup
{
    [After(HookType.Assembly)]
    public static void Cleanup()
    {
        PgTestBase<TheDbContext>.Shutdown();
        PgTestBase<DefaultTimestampDbContext>.Shutdown();
        PgTestBase<TimestampDbContext>.Shutdown();
    }
}
