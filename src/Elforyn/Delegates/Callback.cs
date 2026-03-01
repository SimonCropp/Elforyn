namespace Elforyn;

public delegate Task Callback<in TDbContext>(NpgsqlConnection connection, TDbContext context)
    where TDbContext : DbContext;
