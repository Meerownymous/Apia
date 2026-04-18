

namespace Apia.Scope;

/// <summary>
/// Decorator that applies an <see cref="IScope{TRecord,TFilter}"/> to an aggregate stream.
/// Upgrades <see cref="AllOf{TRecord}"/> to <see cref="LinqQuery{T}"/> when the scope provides
/// a LINQ expression, enabling SQL-level pushdown on backends that support it.
/// Falls back to in-process filtering for all other queries.
/// </summary>
public sealed class ScopeFilteredAggregateSource<TRecord, TFilter>(
    Func<object, IAsyncEnumerable<TRecord>> inner,
    IScope<TRecord, TFilter> scope,
    TFilter filter) : IAggregateSource<TRecord>
{
    public IAsyncEnumerable<TRecord> From(object query)
        => scope.AsLinq(filter).Match(
            linq => query is AllOf<TRecord>
                ? inner(new LinqQuery<TRecord>(linq))
                : inner(query).Where(r => scope.Includes(r, filter)),
            _ => inner(query).Where(r => scope.Includes(r, filter))
        );
}
