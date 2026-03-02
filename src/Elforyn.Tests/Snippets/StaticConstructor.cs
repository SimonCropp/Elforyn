public class StaticConstructor
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region PgStaticConstructor

    public class Tests(StaticConstructor.Tests.Fixture fixture) : IClassFixture<StaticConstructor.Tests.Fixture>
    {
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

        [Fact]
        public async Task Test()
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            await using var database = await pgInstance.Build([entity]);
            Assert.Equal(1, database.Context.TestEntities.Count());
        }

        #endregion
    }
}
