namespace Elforyn;

public partial class PgDatabase<TDbContext>
{
    public Task<T> Single<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        Single(Set<T>(), predicate);

    public Task<T> SingleIgnoreFilters<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        Single(Set<T>().IgnoreQueryFilters(), predicate);

    static Task<T> Single<T>(IQueryable<T> set, Expression<Func<T, bool>>? predicate)
        where T : class
    {
        if (predicate is null)
        {
            return set.SingleAsync();
        }

        return set.SingleAsync(predicate);
    }
}
