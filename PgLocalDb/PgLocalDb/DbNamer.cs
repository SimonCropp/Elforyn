namespace PgLocalDb;

static class DbNamer
{
    public static string DeriveDbName(
        string? suffix,
        string member,
        string testClass)
    {
        if (suffix is null)
        {
            return $"{testClass}_{member}";
        }

        return $"{testClass}_{member}_{suffix}";
    }
}
