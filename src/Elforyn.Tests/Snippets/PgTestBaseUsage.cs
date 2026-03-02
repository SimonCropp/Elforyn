public class PgTestBaseClass
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region PgTestBase

    public abstract class TestBase
    {
        static PgInstance<TheDbContext> pgInstance;

        static TestBase() =>
            pgInstance = new(
                ConnectionSettings.ConnectionString,
                constructInstance: builder => new(builder.Options));

        public static Task<PgDatabase<TheDbContext>> LocalDb(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "") =>
            pgInstance.Build(testFile, databaseSuffix, memberName);
    }

    public class Tests :
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            await using var database = await LocalDb();
            var entity = new TheEntity
            {
                Property = "prop"
            };
            await database.AddData(entity);

            Assert.Equal(1, database.Context.TestEntities.Count());
        }
    }

    #endregion
}
