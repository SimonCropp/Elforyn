using MethodTimer;

class Wrapper : IDisposable
{
    string connectionString;
    string templateName;
    Func<NpgsqlConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
    SemaphoreSlim sharedLock = new(1, 1);
    bool sharedCreated;
    Task startupTask = null!;

    public Wrapper(
        string connectionString,
        string name,
        Func<NpgsqlConnection, Task>? callback = null)
    {
        ElforynLogging.WrapperCreated = true;
        this.connectionString = connectionString;
        templateName = $"elforyn_template_{name}".ToLowerInvariant();
        this.callback = callback;
    }

    [Time("Name: '{name}'")]
    public async Task<NpgsqlConnection> CreateDatabaseFromTemplate(string name)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new("The database name 'template' is reserved.");
        }

        var dbName = $"elforyn_{name}".ToLowerInvariant();

        await startupTask;

        await using (var masterConnection = await OpenMasterConnection())
        {
            await masterConnection.ExecuteCommandAsync($"""DROP DATABASE IF EXISTS "{dbName}" WITH (FORCE)""");
            await masterConnection.ExecuteCommandAsync($"""CREATE DATABASE "{dbName}" TEMPLATE "{templateName}" """);
        }

        var dbConnectionString = ElforynSettings.BuildConnectionString(connectionString, dbName);
        var resultConnection = new NpgsqlConnection(dbConnectionString);
        await resultConnection.OpenAsync();
        return resultConnection;
    }

    public void Start(DateTime timestamp, Func<NpgsqlConnection, Task> buildTemplate)
    {
        var stopwatch = Stopwatch.StartNew();
        InnerStart(timestamp, buildTemplate);
        var message = $"Start `{templateName}` {stopwatch.ElapsedMilliseconds}ms.";
        ElforynLogging.Log(message);
    }

    public Task AwaitStart() => startupTask;

    public async Task<NpgsqlConnection> OpenExistingDatabase(string name)
    {
        await startupTask;
        var dbName = $"elforyn_{name}".ToLowerInvariant();
        var dbConnectionString = ElforynSettings.BuildConnectionString(connectionString, dbName);
        var connection = new NpgsqlConnection(dbConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<NpgsqlConnection> OpenSharedDatabase(
        Func<NpgsqlConnection, Task>? initialize = null)
    {
        await sharedLock.WaitAsync();
        try
        {
            if (!sharedCreated)
            {
                var initConnection = await CreateDatabaseFromTemplate("Shared");
                if (initialize != null)
                {
                    await initialize(initConnection);
                }

                await initConnection.DisposeAsync();
                sharedCreated = true;
            }
        }
        finally
        {
            sharedLock.Release();
        }

        return await OpenExistingDatabase("Shared");
    }

    void InnerStart(DateTime timestamp, Func<NpgsqlConnection, Task> buildTemplate) =>
        startupTask = CreateTemplate(timestamp, buildTemplate);

    [Time("Timestamp: '{timestamp}'")]
    async Task CreateTemplate(
        DateTime timestamp,
        Func<NpgsqlConnection, Task> buildTemplate)
    {
        await using var masterConnection = await OpenMasterConnection();

        ElforynLogging.LogIfVerbose($"ServerVersion: {masterConnection.ServerVersion}");

        // Check if template database exists
        var templateExists = await DatabaseExists(masterConnection, templateName);

        if (templateExists)
        {
            // Check stored timestamp via database comment
            var storedTimestamp = await GetDatabaseTimestamp(masterConnection, templateName);
            if (storedTimestamp == timestamp)
            {
                ElforynLogging.LogIfVerbose("Not modified so skipping rebuild");

                if (callback != null)
                {
                    var templateConnectionString = ElforynSettings.BuildConnectionString(connectionString, templateName);
                    // Need to allow connections to template temporarily
                    await masterConnection.ExecuteCommandAsync($"""ALTER DATABASE "{templateName}" WITH ALLOW_CONNECTIONS true""");
                    await using (var templateConnection = new NpgsqlConnection(templateConnectionString))
                    {
                        await templateConnection.OpenAsync();
                        await callback(templateConnection);
                    }
                    await masterConnection.ExecuteCommandAsync($"""ALTER DATABASE "{templateName}" WITH ALLOW_CONNECTIONS false IS_TEMPLATE true""");
                }

                return;
            }

            // Timestamp mismatch - rebuild
            await DropTemplateDatabase(masterConnection);
        }

        // Create new template database
        await masterConnection.ExecuteCommandAsync($"""CREATE DATABASE "{templateName}" """);

        var connStr = ElforynSettings.BuildConnectionString(connectionString, templateName);
        await using (var templateConnection = new NpgsqlConnection(connStr))
        {
            await templateConnection.OpenAsync();
            await buildTemplate(templateConnection);
            if (callback != null)
            {
                await callback(templateConnection);
            }
        }

        // Store timestamp as a database comment
        var timestampStr = timestamp.ToString("O");
        await masterConnection.ExecuteCommandAsync($"""COMMENT ON DATABASE "{templateName}" IS '{timestampStr}'""");

        // Mark as template and disallow connections
        await masterConnection.ExecuteCommandAsync($"""ALTER DATABASE "{templateName}" WITH ALLOW_CONNECTIONS false IS_TEMPLATE true""");
    }

    static async Task<bool> DatabaseExists(NpgsqlConnection connection, string dbName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        cmd.Parameters.AddWithValue("name", dbName);
        var result = await cmd.ExecuteScalarAsync();
        return result is not null;
    }

    static async Task<DateTime?> GetDatabaseTimestamp(NpgsqlConnection connection, string dbName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_catalog.shobj_description(d.oid, 'pg_database') FROM pg_database d WHERE d.datname = @name";
        cmd.Parameters.AddWithValue("name", dbName);
        var result = await cmd.ExecuteScalarAsync();
        if (result is string comment && DateTime.TryParse(comment, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt;
        }

        return null;
    }

    async Task<NpgsqlConnection> OpenMasterConnection()
    {
        var masterConnectionString = ElforynSettings.BuildConnectionString(connectionString, "postgres");
        var connection = new NpgsqlConnection(masterConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    [Time("dbName: '{dbName}'")]
    public async Task DeleteDatabase(string dbName)
    {
        var fullName = $"elforyn_{dbName}".ToLowerInvariant();
        await using var connection = await OpenMasterConnection();
        await connection.ExecuteCommandAsync($"""DROP DATABASE IF EXISTS "{fullName}" WITH (FORCE)""");
    }

    [Time]
    public async Task DeleteInstance()
    {
        await using var connection = await OpenMasterConnection();
        await DropTemplateDatabase(connection);
        Dispose();
    }

    async Task DropTemplateDatabase(NpgsqlConnection connection)
    {
        if (!await DatabaseExists(connection, templateName))
        {
            return;
        }

        if (await IsTemplate(connection, templateName))
        {
            await connection.ExecuteCommandAsync($"""ALTER DATABASE "{templateName}" IS_TEMPLATE false""");
        }

        await connection.ExecuteCommandAsync($"""DROP DATABASE IF EXISTS "{templateName}" WITH (FORCE)""");
    }

    static async Task<bool> IsTemplate(NpgsqlConnection connection, string dbName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT datistemplate FROM pg_database WHERE datname = @name";
        cmd.Parameters.AddWithValue("name", dbName);
        var result = await cmd.ExecuteScalarAsync();
        return result is true;
    }

    public void Dispose() => semaphoreSlim.Dispose();
}
