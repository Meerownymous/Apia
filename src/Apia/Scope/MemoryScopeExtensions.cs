namespace Apia.Scope;

public static class MemoryScopeExtensions
{
    /// <summary>
    /// Returns an <see cref="IMemory"/> where every registered <see cref="IVaultScope{TRecord,TFilter}"/>
    /// is enforced against <paramref name="filter"/> on Vault loads and Branch saves/deletes.
    /// </summary>
    public static IMemory Scoped<TFilter>(
        this IMemory memory,
        TFilter filter,
        ScopeBuilder<TFilter> builder)
        => new ScopeMemory<TFilter>(memory, builder.Build(), filter);
}
