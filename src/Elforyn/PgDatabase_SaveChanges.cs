namespace Elforyn;

public partial class PgDatabase<TDbContext>
{
    public Task<int> SaveChangesAsync()
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync();
    }

    void ThrowForNoChanges()
    {
        if (!Context.ChangeTracker.HasChanges())
        {
            throw new("No pending changes. It is possible Find or Single has been used, and the returned entity then modified. Find or Single use a non tracking context. Use the Context to dor modifications.");
        }
    }

    public int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowForNoChanges();
        return Context.SaveChanges(acceptAllChangesOnSuccess);
    }

    public Task<int> SaveChangesAsync(Cancel cancel = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(cancel);
    }

    public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, Cancel cancel = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(acceptAllChangesOnSuccess, cancel);
    }
}
