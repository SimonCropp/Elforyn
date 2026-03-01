namespace Elforyn;

public delegate TDbContext ConstructInstance<TDbContext>(DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;
