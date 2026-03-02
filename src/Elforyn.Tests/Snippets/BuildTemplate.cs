public class PgBuildTemplate
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region PgBuildTemplate

    public class BuildTemplate(PgBuildTemplate.BuildTemplate.Fixture fixture) : IClassFixture<PgBuildTemplate.BuildTemplate.Fixture>
    {
        public class Fixture : IAsyncDisposable
        {
            public PgInstance<TheDbContext> PgInstance { get; } = new(
                ConnectionSettings.ConnectionString,
                constructInstance: builder => new(builder.Options),
                buildTemplate: async context =>
                {
                    await context.Database.EnsureCreatedAsync();
                    var entity = new TheEntity
                    {
                        Property = "prop"
                    };
                    context.Add(entity);
                    await context.SaveChangesAsync();
                });

            public async ValueTask DisposeAsync()
            {
                await PgInstance.Cleanup();
                PgInstance.Dispose();
            }
        }

        PgInstance<TheDbContext> pgInstance = fixture.PgInstance;

        [Fact]
        public async Task BuildTemplateTest()
        {
            await using var database = await pgInstance.Build();

            Assert.Equal(1, database.Context.TestEntities.Count());
        }

        #endregion
    }
}
