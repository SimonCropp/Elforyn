public class TimestampTests : PgTestBase<TimestampDbContext>
{
    static string ConnectionString =>
        Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
        "Host=localhost;Username=postgres;Password=postgres";

    static TimestampTests() =>
        Initialize(
            ConnectionString,
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(new() { Id = Guid.NewGuid(), Name = "Template Company" });
                await data.SaveChangesAsync();
            },
            timestamp: Timestamp.LastModified<TimestampDbContext>());

    [Fact]
    public async Task ExplicitTimestamp_UsesDbContextAssemblyTimestamp()
    {
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [Fact]
    public async Task ExplicitTimestamp_TemplateDataPersists()
    {
        var count = await AssertData.Companies.CountAsync();
        Assert.Equal(1, count);
    }
}
