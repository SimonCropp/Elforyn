public class PgBuildTemplate
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region PgBuildTemplate

    public class BuildTemplate
    {
        static string ConnectionString =>
            Environment.GetEnvironmentVariable("Elforyn_ConnectionString") ??
            "Host=localhost;Username=postgres;Password=postgres";

        static PgInstance<TheDbContext> pgInstance;

        static BuildTemplate() =>
            pgInstance = new(
                ConnectionString,
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

        [Fact]
        public async Task BuildTemplateTest()
        {
            await using var database = await pgInstance.Build();

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
