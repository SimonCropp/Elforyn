namespace Elforyn;

#region ElforynLogging

public static class ElforynLogging
{
    public static void EnableVerbose(bool sqlLogging = false)
    {
        if (WrapperCreated)
        {
            throw new("Must be called prior to `PgInstance` being created.");
        }

        Enabled = true;
        SqlLoggingEnabled = sqlLogging;
    }

    internal static bool SqlLoggingEnabled;
    internal static bool Enabled;
    internal static bool WrapperCreated;

    internal static void LogIfVerbose(string message)
    {
        if (Enabled)
        {
            Log(message);
        }
    }

    internal static void Log(string message)
    {
        try
        {
            Console.Error.WriteLine($"Elforyn: {message}");
        }
        // dont care if log fails
        catch
        {
        }
    }
}

#endregion
