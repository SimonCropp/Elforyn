namespace Elforyn;

public partial class PgDatabase<TDbContext>
{
    public Task<int> Count<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        if (predicate is null)
        {
            return Set<T>().CountAsync();
        }

        return Set<T>().CountAsync(predicate);
    }
}
