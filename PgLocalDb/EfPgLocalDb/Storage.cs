namespace EfPgLocalDb;

public readonly struct Storage
{
    public Storage(string name, string directory)
    {
        Name = name;
        Directory = directory;
    }

    public readonly string Name;
    public readonly string Directory;
}
