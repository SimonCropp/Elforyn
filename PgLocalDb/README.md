# PgLocalDb

PostgreSQL wrapper for simplified testing with isolated databases for Entity Framework Core.

## Overview

PgLocalDb provides a wrapper around PostgreSQL to simplify running tests with isolated databases. Each test method gets its own database instance, preventing test interference while maintaining good performance through template databases.

## Why PgLocalDb?

### Goals

- Have an isolated PostgreSQL database for each unit test method
- Minimal performance impact through template database reuse
- Easy debugging with real databases that can be inspected with pgAdmin or other tools
- Cross-platform support (unlike SQL Server LocalDB which is Windows-only)

### Why not SQLite?

- PostgreSQL and SQLite have different feature sets and incompatible query languages
- Testing against PostgreSQL ensures your code works with your production database

### Why not InMemory Provider?

- Difficult to debug - you can't inspect the database state with external tools
- InMemory provider doesn't support the full PostgreSQL feature set
- InMemory uses shared mutable state which can cause issues with parallel tests
- See: [InMemory is not a relational database](https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory#inmemory-is-not-a-relational-database)

## Installation

```bash
# For raw PostgreSQL connection support
dotnet add package PgLocalDb

# For Entity Framework Core support
dotnet add package EfPgLocalDb
```

## Prerequisites

- PostgreSQL installed and running locally
- Default connection settings: `localhost:5432`, user: `postgres`, password: `postgres`

## Configuration

You can configure the PostgreSQL connection settings:

```csharp
using PgLocalDb;

// Configure connection settings (optional)
PgSettings.Host = "localhost";
PgSettings.Port = 5432;
PgSettings.Username = "postgres";
PgSettings.Password = "yourpassword";
```

## Usage

### Raw PostgreSQL Connection

```csharp
using PgLocalDb;

public class MyTests
{
    static PgInstance pgInstance = new(
        "MyTestInstance",
        async connection =>
        {
            // Build your template database schema
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE Users (
                    Id SERIAL PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(100) NOT NULL
                )";
            await command.ExecuteNonQueryAsync();
        });

    [Fact]
    public async Task MyTest()
    {
        // Each test gets its own database created from the template
        await using var database = await pgInstance.Build();

        // Use the database
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
        command.Parameters.AddWithValue("name", "John Doe");
        command.Parameters.AddWithValue("email", "john@example.com");
        await command.ExecuteNonQueryAsync();

        // database is automatically cleaned up at the end
    }
}
```

### Entity Framework Core

```csharp
using EfPgLocalDb;
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class MyEfTests
{
    static PgInstance<MyDbContext> pgInstance = new(
        builder => new MyDbContext(builder.Options),
        async context =>
        {
            // Build your template database schema
            await context.Database.EnsureCreatedAsync();
        });

    [Fact]
    public async Task MyTest()
    {
        await using var database = await pgInstance.Build();

        // Use the DbContext
        database.Context.Users.Add(new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        });
        await database.Context.SaveChangesAsync();

        var user = await database.Context.Users.FirstAsync();
        Assert.Equal("John Doe", user.Name);
    }
}
```

### With Initial Data

```csharp
[Fact]
public async Task TestWithData()
{
    var users = new[]
    {
        new User { Name = "Alice", Email = "alice@example.com" },
        new User { Name = "Bob", Email = "bob@example.com" }
    };

    await using var database = await pgInstance.Build(data: users);

    var count = await database.Context.Users.CountAsync();
    Assert.Equal(2, count);
}
```

### Multiple Contexts

`PgDatabase<TDbContext>` provides multiple DbContext instances:

- `Context`: Default tracked context
- `NoTrackingContext`: No-tracking context for read-only queries
- `NewDbContext()`: Create a new context with custom tracking behavior
- `NewConnectionOwnedDbContext()`: Create a new context with its own connection

```csharp
[Fact]
public async Task UsingMultipleContexts()
{
    await using var database = await pgInstance.Build();

    // Use default tracking context for writes
    database.Context.Users.Add(new User { Name = "Test", Email = "test@example.com" });
    await database.Context.SaveChangesAsync();

    // Use no-tracking context for reads
    var users = await database.NoTrackingContext.Users.ToListAsync();

    // Or create a custom context
    var customContext = database.NewDbContext(QueryTrackingBehavior.NoTracking);
}
```

## How It Works

1. **Template Database**: On first use, PgLocalDb creates a "template" database using your `buildTemplate` function
2. **Database Creation**: Each test gets a new database created using PostgreSQL's `CREATE DATABASE ... WITH TEMPLATE` command
3. **Timestamp Tracking**: The template is rebuilt only when your code changes (detected via assembly modification time or custom timestamp)
4. **Automatic Cleanup**: Databases are automatically dropped when disposed or can be explicitly cleaned up

## Database Naming

Databases are automatically named based on the test class and method:

```csharp
// Database will be named: pglocaldb_MyTestInstance_MyTests_MyTestMethod
[Fact]
public async Task MyTestMethod()
{
    await using var database = await pgInstance.Build();
    // ...
}

// You can also provide an explicit name
[Fact]
public async Task CustomName()
{
    await using var database = await pgInstance.Build("my_custom_db_name");
    // ...
}
```

## Storage Location

By default, timestamp files are stored in:
- Windows: `C:\Users\{username}\AppData\Local\Temp\PgLocalDb\{instance-name}\`
- Linux/Mac: `/tmp/PgLocalDb/{instance-name}/`

You can customize this with the `PgLocalDbData` environment variable:

```bash
export PgLocalDbData=/path/to/custom/location
```

## Cleanup

```csharp
// Cleanup is automatic when using 'await using'
await using var database = await pgInstance.Build();

// Or manually cleanup
await database.Delete();

// Cleanup all databases for an instance
await pgInstance.Cleanup();
```

## Comparison with LocalDb (SQL Server)

| Feature | PgLocalDb | LocalDb |
|---------|-----------|---------|
| Cross-platform | ✅ Yes | ❌ Windows only |
| Database Engine | PostgreSQL | SQL Server |
| Installation | PostgreSQL required | LocalDB required |
| Template Method | `CREATE DATABASE WITH TEMPLATE` | File copy (.mdf/.ldf) |
| Connection String | Standard PostgreSQL | LocalDB-specific |

## Advanced Configuration

### Custom PostgreSQL Options

```csharp
var pgInstance = new PgInstance<MyDbContext>(
    builder => new MyDbContext(builder.Options),
    buildTemplate: async context => await context.Database.EnsureCreatedAsync(),
    npgsqlOptionsBuilder: options =>
    {
        options.EnableRetryOnFailure(maxRetryCount: 3);
        options.CommandTimeout(30);
    });
```

### Custom Timestamp

```csharp
var pgInstance = new PgInstance(
    "MyInstance",
    buildTemplate: async connection => { /* ... */ },
    timestamp: new DateTime(2024, 1, 1)); // Template rebuilds when this changes
```

### Custom Storage Location

```csharp
var storage = new Storage("MyInstance", @"C:\MyCustomPath");
var pgInstance = new PgInstance<MyDbContext>(
    builder => new MyDbContext(builder.Options),
    storage: storage);
```

## License

MIT

## Credits

Inspired by [LocalDb](https://github.com/SimonCropp/LocalDb) by Simon Cropp.
