namespace EfPgLocalDb;

static class BuildTemplateConverter
{
    public static TemplateFromConnection<TDbContext> Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext
    {
        if (buildTemplate is null)
        {
            return async (_, builder) =>
            {
                await using var context = constructInstance(builder);
                await context.Database.EnsureCreatedAsync();
            };
        }

        return async (_, builder) =>
        {
            await using var context = constructInstance(builder);
            await buildTemplate(context);
        };
    }
}
