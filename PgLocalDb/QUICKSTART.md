# Quick Start Guide

## Prerequisites

1. Install PostgreSQL:
   ```bash
   # Windows (using Chocolatey)
   choco install postgresql

   # Mac (using Homebrew)
   brew install postgresql
   brew services start postgresql

   # Linux (Ubuntu/Debian)
   sudo apt-get install postgresql
   ```

2. Ensure PostgreSQL is running and accessible at `localhost:5432` with user `postgres`

## Basic Setup

### 1. Install the NuGet package

```bash
dotnet add package EfPgLocalDb
```

### 2. Create your DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 3. Create a test with PgLocalDb

```csharp
using EfPgLocalDb;
using Xunit;

public class UserTests
{
    // Create one instance per test class
    static PgInstance<MyDbContext> pgInstance = new(
        builder => new MyDbContext(builder.Options),
        async context => await context.Database.EnsureCreatedAsync()
    );

    [Fact]
    public async Task CanAddUser()
    {
        // Each test gets its own isolated database
        await using var database = await pgInstance.Build();

        // Use the context
        database.Context.Users.Add(new User
        {
            Name = "Alice",
            Email = "alice@example.com"
        });
        await database.Context.SaveChangesAsync();

        // Verify
        var user = await database.Context.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task CanQueryUsers()
    {
        // This test gets its own database - no interference!
        await using var database = await pgInstance.Build();

        database.Context.Users.AddRange(
            new User { Name = "Bob", Email = "bob@example.com" },
            new User { Name = "Charlie", Email = "charlie@example.com" }
        );
        await database.Context.SaveChangesAsync();

        var count = await database.Context.Users.CountAsync();
        Assert.Equal(2, count);
    }
}
```

### 4. Run your tests

```bash
dotnet test
```

## Configuration

If your PostgreSQL uses different credentials:

```csharp
using PgLocalDb;

// Add this before creating PgInstance
PgSettings.Host = "localhost";
PgSettings.Port = 5432;
PgSettings.Username = "myuser";
PgSettings.Password = "mypassword";

static PgInstance<MyDbContext> pgInstance = new(/* ... */);
```

## Key Concepts

1. **PgInstance**: Create one per test class (static field). It manages the template database.

2. **Build()**: Call this in each test method to get an isolated database.

3. **Template Database**: Created once and reused for all tests. Rebuilt only when your code changes.

4. **Auto Cleanup**: Databases are automatically dropped when you use `await using`.

## Next Steps

- Read the [full README](README.md) for advanced features
- Check out the test projects for more examples
- Explore different ways to initialize test data

## Troubleshooting

**Connection errors?**
- Verify PostgreSQL is running: `pg_isready`
- Check your connection settings in `PgSettings`
- Ensure the `postgres` user has permission to create databases

**Tests running slowly?**
- The first test run creates the template database (slower)
- Subsequent tests reuse the template (fast)
- Template is only rebuilt when your code changes

**Databases not being cleaned up?**
- Make sure you use `await using var database = ...`
- Or call `await database.Delete()` explicitly
