namespace Elforyn;

public struct Storage
{
    public static Storage FromSuffix<TDbContext>(string suffix)
    {
        Ensure.NotNullOrWhiteSpace(suffix);
        var instanceName = GetInstanceName<TDbContext>(suffix);
        return new(instanceName);
    }

    public Storage(string name)
    {
        Ensure.NotNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }

    static string GetInstanceName<TDbContext>(string? scopeSuffix)
    {
        Ensure.NotWhiteSpace(scopeSuffix);

        #region GetInstanceName

        if (scopeSuffix is null)
        {
            return typeof(TDbContext).Name;
        }

        return $"{typeof(TDbContext).Name}_{scopeSuffix}";

        #endregion
    }
}
