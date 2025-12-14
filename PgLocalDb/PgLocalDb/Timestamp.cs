namespace PgLocalDb;

static class Timestamp
{
    public static DateTime LastModified(Delegate @delegate)
    {
        var assembly = @delegate.Method.DeclaringType!.Assembly;
        return LastModified(assembly);
    }

    public static DateTime LastModified(Assembly assembly)
    {
        var location = assembly.Location;
        if (string.IsNullOrEmpty(location))
        {
            return DateTime.UtcNow;
        }

        return File.GetLastWriteTimeUtc(location);
    }

    public static DateTime LastModified<T>()
    {
        var assembly = typeof(T).Assembly;
        return LastModified(assembly);
    }
}
