[TestFixture]
public class DefaultTimestampTests :
    PgTestBase<DefaultTimestampDbContext>
{
    static string ConnectionString =>
        Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
        "Host=localhost;Username=postgres;Password=postgres";

    static DefaultTimestampTests() =>
        Initialize(
            ConnectionString,
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default Template Company"
                    });
                await data.SaveChangesAsync();
            });
    // Note: No explicit timestamp provided - should use default behavior

    [Test]
    public async Task NoExplicitTimestamp_UsesDefaultBehavior()
    {
        // Template should have been built with default timestamp behavior
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [Test]
    public async Task NoExplicitTimestamp_TemplateDataPersists()
    {
        // The template company from Initialize should exist
        var count = await AssertData.Companies.CountAsync();
        That(count, Is.EqualTo(1));
    }
}
