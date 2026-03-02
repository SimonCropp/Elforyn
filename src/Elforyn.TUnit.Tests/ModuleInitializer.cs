public static class ModuleInitializer
{
    static string ConnectionString =>
        Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
        "Host=localhost;Username=postgres;Password=postgres";

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        ElforynLogging.EnableVerbose();
        ElforynSettings.ConnectionBuilder(_ => _.Timeout = 300);
        PgTestBase<TheDbContext>.Initialize(ConnectionString);
    }
}
