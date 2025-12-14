namespace PgLocalDb;

class Wrapper
{
    public readonly string Directory;
    Func<DbConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
    public readonly string MasterConnectionString;
    Func<string, DbConnection> buildConnection;
    string instance;
    string TemplateConnectionString;
    string templateDbName;
    Task startupTask = null!;
    HashSet<string> createdDatabases = new();

    public Wrapper(
        Func<string, DbConnection> buildConnection,
        string instance,
        string directory,
        Func<DbConnection, Task>? callback = null)
    {
        Guard.AgainstInvalidFileName(instance);

        this.buildConnection = buildConnection;
        this.instance = instance;
        templateDbName = $"pglocaldb_{instance}_template";
        MasterConnectionString = PgSettings.BuildConnectionString("postgres");
        TemplateConnectionString = PgSettings.BuildConnectionString(templateDbName);
        Directory = directory;
        this.callback = callback;

        System.IO.Directory.CreateDirectory(directory);
    }

    public async Task<string> CreateDatabaseFromTemplate(string name)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new("The database name 'template' is reserved.");
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
        {
            throw new ArgumentException($"Invalid database name. Name must be valid to use as a file name. Value: {name}", nameof(name));
        }

        await startupTask;

        var dbName = $"pglocaldb_{instance}_{name}";
        createdDatabases.Add(dbName);

        await using var masterConnection = await OpenMasterConnection();

        // Terminate existing connections to the database if it exists
        await TerminateDatabaseConnections(masterConnection, dbName);

        // Drop the database if it exists
        await DropDatabaseIfExists(masterConnection, dbName);

        // Create new database from template
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\" WITH TEMPLATE \"{templateDbName}\"", masterConnection);
        await createCommand.ExecuteNonQueryAsync();

        var connectionString = PgSettings.BuildConnectionString(dbName);
        await RunCallback(connectionString);
        return connectionString;
    }

    async Task RunCallback(string connectionString)
    {
        if (callback is null)
        {
            return;
        }

        try
        {
            await semaphoreSlim.WaitAsync();
            if (callback is null)
            {
                return;
            }

            await using var connection = buildConnection(connectionString);
            await connection.OpenAsync();
            await callback(connection);
            callback = null;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void Start(DateTime timestamp, Func<DbConnection, Task> buildTemplate)
    {
        var stopwatch = Stopwatch.StartNew();
        startupTask = InnerStart(timestamp, buildTemplate);
        startupTask.Wait();
        Trace.WriteLine($"PgLocalDb started in {stopwatch.ElapsedMilliseconds}ms");
    }

    async Task InnerStart(DateTime timestamp, Func<DbConnection, Task> buildTemplate)
    {
        await using var masterConnection = await OpenMasterConnection();

        // Check if template database exists and needs rebuilding
        var shouldRebuild = ShouldRebuildTemplate(timestamp);

        if (shouldRebuild)
        {
            await TerminateDatabaseConnections(masterConnection, templateDbName);
            await DropDatabaseIfExists(masterConnection, templateDbName);

            // Create the template database
            await using (var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{templateDbName}\"", masterConnection))
            {
                await createCommand.ExecuteNonQueryAsync();
            }

            // Build the template
            await using var templateConnection = await OpenTemplateConnection();
            await buildTemplate(templateConnection);

            // Mark the database as a template
            await using (var masterConn = await OpenMasterConnection())
            {
                await using var updateCommand = new NpgsqlCommand(
                    $"UPDATE pg_database SET datistemplate = TRUE WHERE datname = '{templateDbName}'",
                    masterConn);
                await updateCommand.ExecuteNonQueryAsync();
            }

            // Save timestamp
            SaveTimestamp(timestamp);
        }
    }

    bool ShouldRebuildTemplate(DateTime timestamp)
    {
        var timestampFile = GetTimestampFilePath();
        if (!File.Exists(timestampFile))
        {
            return true;
        }

        var savedTimestamp = File.ReadAllText(timestampFile);
        return savedTimestamp != timestamp.ToString("O");
    }

    void SaveTimestamp(DateTime timestamp)
    {
        var timestampFile = GetTimestampFilePath();
        File.WriteAllText(timestampFile, timestamp.ToString("O"));
    }

    string GetTimestampFilePath() => Path.Combine(Directory, "template.timestamp");

    async Task<NpgsqlConnection> OpenMasterConnection()
    {
        var connection = new NpgsqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    async Task<NpgsqlConnection> OpenTemplateConnection()
    {
        var connection = new NpgsqlConnection(TemplateConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    async Task TerminateDatabaseConnections(NpgsqlConnection masterConnection, string dbName)
    {
        await using var terminateCommand = new NpgsqlCommand(
            $@"SELECT pg_terminate_backend(pg_stat_activity.pid)
               FROM pg_stat_activity
               WHERE pg_stat_activity.datname = '{dbName}'
               AND pid <> pg_backend_pid()",
            masterConnection);
        await terminateCommand.ExecuteNonQueryAsync();
    }

    async Task DropDatabaseIfExists(NpgsqlConnection masterConnection, string dbName)
    {
        await using var dropCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\"", masterConnection);
        await dropCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteDatabase(string name)
    {
        var dbName = $"pglocaldb_{instance}_{name}";
        await using var masterConnection = await OpenMasterConnection();
        await TerminateDatabaseConnections(masterConnection, dbName);
        await DropDatabaseIfExists(masterConnection, dbName);
        createdDatabases.Remove(dbName);
    }

    public async Task Cleanup()
    {
        await using var masterConnection = await OpenMasterConnection();

        // Delete all created databases
        foreach (var dbName in createdDatabases.ToList())
        {
            await TerminateDatabaseConnections(masterConnection, dbName);
            await DropDatabaseIfExists(masterConnection, dbName);
        }

        createdDatabases.Clear();

        // Delete the template database
        await TerminateDatabaseConnections(masterConnection, templateDbName);
        await DropDatabaseIfExists(masterConnection, templateDbName);

        // Delete the directory
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }
}
