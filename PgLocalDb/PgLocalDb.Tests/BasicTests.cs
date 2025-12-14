namespace PgLocalDb.Tests;

public class BasicTests
{
    static PgInstance pgInstance = new(
        "PgLocalDbTests",
        async connection =>
        {
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
    public async Task CanCreateDatabase()
    {
        await using var database = await pgInstance.Build();
        Assert.NotNull(database);
        Assert.NotNull(database.Connection);
        Assert.NotNull(database.ConnectionString);
    }

    [Fact]
    public async Task CanInsertAndQueryData()
    {
        await using var database = await pgInstance.Build();

        // Insert data
        await using (var command = database.Connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            command.Parameters.AddWithValue("name", "John Doe");
            command.Parameters.AddWithValue("email", "john@example.com");
            await command.ExecuteNonQueryAsync();
        }

        // Query data
        await using (var command = database.Connection.CreateCommand())
        {
            command.CommandText = "SELECT Name, Email FROM Users WHERE Name = @name";
            command.Parameters.AddWithValue("name", "John Doe");
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("John Doe", reader.GetString(0));
            Assert.Equal("john@example.com", reader.GetString(1));
        }
    }

    [Fact]
    public async Task DatabasesAreIsolated()
    {
        await using var database1 = await pgInstance.Build();
        await using var database2 = await pgInstance.Build();

        // Insert into database1
        await using (var command = database1.Connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            command.Parameters.AddWithValue("name", "User 1");
            command.Parameters.AddWithValue("email", "user1@example.com");
            await command.ExecuteNonQueryAsync();
        }

        // Insert into database2
        await using (var command = database2.Connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            command.Parameters.AddWithValue("name", "User 2");
            command.Parameters.AddWithValue("email", "user2@example.com");
            await command.ExecuteNonQueryAsync();
        }

        // Verify database1 only has User 1
        await using (var command = database1.Connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM Users";
            var count = (long)(await command.ExecuteScalarAsync())!;
            Assert.Equal(1, count);
        }

        // Verify database2 only has User 2
        await using (var command = database2.Connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM Users";
            var count = (long)(await command.ExecuteScalarAsync())!;
            Assert.Equal(1, count);
        }
    }
}
