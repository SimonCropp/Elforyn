namespace EfPgLocalDb;

public delegate Task TemplateFromConnection<TDbContext>(DbConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;
