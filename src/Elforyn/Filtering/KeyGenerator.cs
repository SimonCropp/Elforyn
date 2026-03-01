#pragma warning disable EF1001
// ReSharper disable once ClassNeverInstantiated.Global
class KeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies, RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies) :
    NpgsqlCompiledQueryCacheKeyGenerator(dependencies, relationalDependencies)
{
    public override object GenerateCacheKey(Expression query, bool async)
        => new NpgsqlCompiledQueryCacheKey(
            GenerateCacheKeyCore(query, async),
            QueryFilter.IsEnabled);

    readonly struct NpgsqlCompiledQueryCacheKey(
        RelationalCompiledQueryCacheKey relationalKey,
        bool queryFilterEnabled) :
        IEquatable<NpgsqlCompiledQueryCacheKey>
    {
        readonly RelationalCompiledQueryCacheKey relationalKey = relationalKey;
        readonly bool queryFilterEnabled = queryFilterEnabled;

        public override bool Equals(object? obj)
            => obj is NpgsqlCompiledQueryCacheKey key &&
               Equals(key);

        public bool Equals(NpgsqlCompiledQueryCacheKey other)
            => relationalKey.Equals(other.relationalKey) &&
               queryFilterEnabled == other.queryFilterEnabled;

        public override int GetHashCode()
            => HashCode.Combine(relationalKey, queryFilterEnabled);
    }
}
