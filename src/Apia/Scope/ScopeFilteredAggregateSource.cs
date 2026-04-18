using System.Linq.Expressions;

namespace Apia.Scope;

/// <summary>
/// Decorator that applies an <see cref="IScope{TRecord,TFilter}"/> to an aggregate stream.
/// Upgrades <see cref="AllOf{TRecord}"/> to <see cref="LinqQuery{T}"/> when the scope provides
/// a LINQ expression, enabling SQL-level pushdown on backends that support it.
/// Falls back to in-process filtering for all other queries.
/// </summary>
public sealed class ScopeFilteredAggregateSource<TRecord, TFilter>(
    IMemory inner,
    IScope<TRecord, TFilter> scope,
    TFilter filter) : IAggregateSource<TRecord>
{
    public IAsyncEnumerable<TRecord> From<TQuery>(IQuery<TQuery, TRecord> query)
        => scope.AsLinq(filter).Match(
            linq => query is AllOf<TRecord>
                ? inner.Aggregate<TRecord, Expression<Func<TRecord, bool>>>(new LinqQuery<TRecord>(linq))
                : inner.Aggregate<TRecord, TQuery>(query).Where(r => scope.Includes(r, filter)),
            _ => inner.Aggregate<TRecord, TQuery>(query).Where(r => scope.Includes(r, filter))
        );
}
