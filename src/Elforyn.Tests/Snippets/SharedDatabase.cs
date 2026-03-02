public class SharedDatabase(SharedDatabase.Fixture fixture) : IClassFixture<SharedDatabase.Fixture>
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    public class Fixture : IAsyncDisposable
    {
        public PgInstance<TheDbContext> PgInstance { get; } = new(
            ConnectionSettings.ConnectionString,
            builder => new(builder.Options));

        public async ValueTask DisposeAsync()
        {
            await PgInstance.Cleanup();
            PgInstance.Dispose();
        }
    }

    PgInstance<TheDbContext> pgInstance = fixture.PgInstance;

    #region PgSharedDatabase

    [Fact]
    public async Task SharedDatabaseTest()
    {
        await using var database = await pgInstance.BuildShared();

        Assert.Equal(0, database.Context.TestEntities.Count());
    }

    #endregion

    #region PgSharedDatabase_WithTransaction

    [Fact]
    public async Task SharedDatabaseWithTransactionTest()
    {
        await using var database = await pgInstance.BuildShared(useTransaction: true);

        database.Context.TestEntities.Add(
            new()
            {
                Property = "prop"
            });
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, database.Context.TestEntities.Count());
    }

    #endregion
}
