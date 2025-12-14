namespace EfPgLocalDb.Tests;

public class EfBasicTests
{
    static PgInstance<TestDbContext> pgInstance = new(
        builder => new TestDbContext(builder.Options),
        async context =>
        {
            await context.Database.EnsureCreatedAsync();
        });

    [Fact]
    public async Task CanCreateDatabase()
    {
        await using var database = await pgInstance.Build();
        Assert.NotNull(database);
        Assert.NotNull(database.Context);
        Assert.NotNull(database.Connection);
    }

    [Fact]
    public async Task CanInsertAndQueryData()
    {
        await using var database = await pgInstance.Build();

        // Insert data
        database.Context.Users.Add(new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        });
        await database.Context.SaveChangesAsync();

        // Query data
        var user = await database.Context.Users.FirstOrDefaultAsync(u => u.Name == "John Doe");
        Assert.NotNull(user);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public async Task DatabasesAreIsolated()
    {
        await using var database1 = await pgInstance.Build(databaseSuffix: "db1");
        await using var database2 = await pgInstance.Build(databaseSuffix: "db2");

        // Insert into database1
        database1.Context.Users.Add(new User { Name = "User 1", Email = "user1@example.com" });
        await database1.Context.SaveChangesAsync();

        // Insert into database2
        database2.Context.Users.Add(new User { Name = "User 2", Email = "user2@example.com" });
        await database2.Context.SaveChangesAsync();

        // Verify database1 only has User 1
        Assert.Equal(1, await database1.Context.Users.CountAsync());
        Assert.Equal("User 1", (await database1.Context.Users.FirstAsync()).Name);

        // Verify database2 only has User 2
        Assert.Equal(1, await database2.Context.Users.CountAsync());
        Assert.Equal("User 2", (await database2.Context.Users.FirstAsync()).Name);
    }

    [Fact]
    public async Task CanUseInitialData()
    {
        var users = new[]
        {
            new User { Name = "Alice", Email = "alice@example.com" },
            new User { Name = "Bob", Email = "bob@example.com" }
        };

        await using var database = await pgInstance.Build(data: users);

        var count = await database.Context.Users.CountAsync();
        Assert.Equal(2, count);

        var alice = await database.Context.Users.FirstOrDefaultAsync(u => u.Name == "Alice");
        Assert.NotNull(alice);
        Assert.Equal("alice@example.com", alice.Email);
    }

    [Fact]
    public async Task NoTrackingContextDoesNotTrackEntities()
    {
        await using var database = await pgInstance.Build();

        // Add a user
        database.Context.Users.Add(new User { Name = "Test User", Email = "test@example.com" });
        await database.Context.SaveChangesAsync();

        // Query with NoTrackingContext
        var user = await database.NoTrackingContext.Users.FirstAsync();
        user.Email = "changed@example.com";

        // Verify the change is not tracked
        Assert.Empty(database.NoTrackingContext.ChangeTracker.Entries());

        // Verify the change is not persisted
        var userFromDb = await database.Context.Users.FindAsync(user.Id);
        Assert.Equal("test@example.com", userFromDb!.Email);
    }
}
