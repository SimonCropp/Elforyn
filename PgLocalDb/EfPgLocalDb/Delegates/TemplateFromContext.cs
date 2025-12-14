namespace EfPgLocalDb;

public delegate Task TemplateFromContext<in TDbContext>(TDbContext context)
    where TDbContext : DbContext;
