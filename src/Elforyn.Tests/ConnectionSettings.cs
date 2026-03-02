public static class ConnectionSettings
{
    [ModuleInitializer]
    public static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("AppVeyor") == "True")
        {
            ConnectionString = "Username=postgres;Password=Password12!;Host=localhost";
        }
        else
        {
            ConnectionString = Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
                               "Username=postgres;Password=postgres;Host=localhost";
        }

        ElforynSettings.ConnectionBuilder(_ => _.Timeout = 300);
    }

    public static string ConnectionString { get; set; }
}
