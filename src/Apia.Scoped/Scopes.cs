namespace Apia.Scoped;

/// <summary>
/// The registered entity scopes for a filter type <typeparamref name="TFilter"/>.
///
/// <para>
/// Build once at application startup and reuse across requests by passing a concrete
/// filter value (e.g. the current user) to <see cref="MemoryScopeExtensions.Scoped{TFilter}"/>.
/// </para>
///
/// <code>
/// // Startup:
/// IScopes&lt;UserContext&gt; scopes = new Scopes&lt;UserContext&gt;()
///     .Register&lt;Post&gt;(new UserPosts())
///     .Register&lt;Comment&gt;(new UserComments());
///
/// // Per-request:
/// IMemory userMemory = memory.Scoped(new UserContext(userId), scopes);
/// </code>
/// </summary>
public sealed class Scopes<TFilter> : IScopes<TFilter>
{
    private readonly Dictionary<Type, object> scopes = new();

    /// <inheritdoc/>
    public IScopes<TFilter> Register<TRecord>(IEntitiesScope<TRecord, TFilter> scope)
        where TRecord : notnull
    {
        scopes[typeof(TRecord)] = scope;
        return this;
    }

    /// <inheritdoc/>
    public bool Has<TRecord>() where TRecord : notnull
        => scopes.ContainsKey(typeof(TRecord));

    /// <inheritdoc/>
    public IEntitiesScope<TRecord, TFilter> For<TRecord>() where TRecord : notnull
        => (IEntitiesScope<TRecord, TFilter>)scopes[typeof(TRecord)];
}
