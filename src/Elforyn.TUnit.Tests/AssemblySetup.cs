public static class AssemblySetup
{
    [After(HookType.Assembly)]
    public static async Task Cleanup()
    {
        await PgTestBase<TheDbContext>.Shutdown();
        await PgTestBase<DefaultTimestampDbContext>.Shutdown();
        await PgTestBase<TimestampDbContext>.Shutdown();
    }
}
