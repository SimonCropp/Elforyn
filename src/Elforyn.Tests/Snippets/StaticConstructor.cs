public class StaticConstructor
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region PgStaticConstructor

    public class Tests
    {
        static PgInstance<TheDbContext> pgInstance;

        static Tests() =>
            pgInstance = new(
                ConnectionSettings.ConnectionString,
                builder => new(builder.Options));

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

        [Fact]
        public async Task Cleanup()
        {
            await pgInstance.Cleanup();
            pgInstance.Dispose();
        }
    }
}
