namespace Elforyn;

public delegate Task TemplateFromConnection<TDbContext>(NpgsqlConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;
