namespace PgLocalDb;

static class DirectoryCleaner
{
    public static void CleanRoot(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            CleanInstance(directory);
        }
    }

    public static void CleanInstance(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore files that are in use
            }
        }
    }
}
