#region MethodTimeLogger

static class MethodTimeLogger
{
    public static void Log(MethodBase method, long milliseconds, string? message)
    {
        if (!ElforynLogging.Enabled)
        {
            return;
        }

        if (message is null)
        {
            ElforynLogging.Log($"{method.Name} {milliseconds}ms");
            return;
        }

        ElforynLogging.Log($"{method.Name} {milliseconds}ms {message}");
    }
}

#endregion
