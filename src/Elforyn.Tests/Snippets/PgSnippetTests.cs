#region PgSnippetTests
public class PgSnippetTests(PgSnippetTests.Fixture fixture) : IClassFixture<PgSnippetTests.Fixture>
{
    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TheEntity>();
    }

    public class Fixture : IAsyncDisposable
    {
        public PgInstance<MyDbContext> PgInstance { get; } = new(
            ConnectionSettings.ConnectionString,
            builder => new(builder.Options));

        public async ValueTask DisposeAsync()
        {
            await PgInstance.Cleanup();
            PgInstance.Dispose();
        }
    }

    PgInstance<MyDbContext> pgInstance = fixture.PgInstance;

    #region PgTest

    [Fact]
    public async Task TheTest()
    {
        #region PgBuildDatabase

        await using var database = await pgInstance.Build();

        #endregion

        #region PgBuildContext

        await using (var data = database.NewDbContext())
        {

            #endregion

            var entity = new TheEntity
            {
                Property = "prop"
            };
            data.Add(entity);
            await data.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var data = database.NewDbContext())
        {
            Assert.Equal(1, data.TestEntities.Count());
        }

        #endregion
    }

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region PgWithDbName

        await using var database = await pgInstance.Build("TheTestWithDbName");

        #endregion

        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Equal(1, database.Context.TestEntities.Count());
    }

    #endregion
}
