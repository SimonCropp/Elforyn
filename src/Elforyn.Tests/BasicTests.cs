public class BasicTests
{
    static string ConnectionString =>
        Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
        "Host=localhost;Username=postgres;Password=postgres";

    [Fact]
    public async Task BuildAndQuery()
    {
        var instance = new PgInstance<TestDbContext>(
            ConnectionString,
            constructInstance: builder => new(builder.Options));

        await using var database = await instance.Build("BuildAndQuery", null);

        database.Context.TestEntities.Add(
            new()
            {
                Name = "Test1"
            });
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await database.Count<TestEntity>();
        Assert.Equal(1, count);

        var entity = await database.Single<TestEntity>();
        Assert.Equal("Test1", entity.Name);

        instance.Dispose();
    }

    [Fact]
    public async Task BuildWithData()
    {
        var instance = new PgInstance<TestDbContext>(
            ConnectionString,
            constructInstance: builder => new(builder.Options));

        var data = new object[]
        {
            new TestEntity
            {
                Name = "Seeded"
            }
        };

        await using var database = await instance.Build("BuildWithData", data);

        var any = await database.Any<TestEntity>(_ => _.Name == "Seeded");
        Assert.True(any);

        instance.Dispose();
    }
}
