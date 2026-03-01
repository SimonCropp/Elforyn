using Microsoft.EntityFrameworkCore.Query;

namespace Elforyn;

/// <summary>
///  Wraps <see cref="IIncludableQueryable{TEntity,TProperty}"/>
/// </summary>
public class IncludableQueryableSettingsTask<TEntity, TProperty> : SettingsTask
    where TEntity : class
{
    internal VerifySettings? Settings { get; }
    internal IIncludableQueryable<TEntity, TProperty> Inner { get; }
    internal Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> Query { get; }

    internal IncludableQueryableSettingsTask(IIncludableQueryable<TEntity, TProperty> source, VerifySettings? settings, Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query)
        : base(settings, _ => query(_, source))
    {
        Inner = source;
        Query = query;
        Settings = settings;
    }
}

/// <summary>
/// Designed to mirror <see cref="EntityFrameworkQueryableExtensions"/>
/// </summary>
public static class IncludableQueryableSettingsTaskExtensions
{
    public static IncludableQueryableSettingsTask<TEntity, TProperty> Include<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, TPreviousProperty> source,
        Expression<Func<TEntity, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.Include(property), source.Settings, source.Query);

    public static IncludableQueryableSettingsTask<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.ThenInclude(property), source.Settings, source.Query);

    public static IncludableQueryableSettingsTask<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, List<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.ThenInclude(property), source.Settings, source.Query);
}
