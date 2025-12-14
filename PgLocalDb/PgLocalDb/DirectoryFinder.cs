namespace PgLocalDb;

static class DirectoryFinder
{
    public static string dataRoot;

    static DirectoryFinder()
    {
        dataRoot = FindDataRoot();
        DirectoryCleaner.CleanRoot(dataRoot);
    }

    public static string Find(string instanceName) => Path.Combine(dataRoot, instanceName);

    public static void Delete(string instanceName)
    {
        var directory = Find(instanceName);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    static string FindDataRoot()
    {
        var pgLocalDbEnv = Environment.GetEnvironmentVariable("PgLocalDbData");
        if (pgLocalDbEnv is not null)
        {
            return pgLocalDbEnv;
        }

        return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "PgLocalDb"));
    }
}
