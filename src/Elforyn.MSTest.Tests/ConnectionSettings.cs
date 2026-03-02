public static class ConnectionSettings
{
    [ModuleInitializer]
    public static void Initialize()
    {
        string connectionString;
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            connectionString = "Username=postgres;Password=Password12!;Host=localhost";
        }
        else
        {
            connectionString = Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
                               "Username=postgres;Password=postgres;Host=localhost";
        }

        connectionString += ";Include Error Detail=True";

        ElforynSettings.ConnectionBuilder(_ => _.Timeout = 300);
        PgTestBase<TheDbContext>.Initialize(connectionString);
    }
}
