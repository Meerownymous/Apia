using System.Linq.Expressions;

namespace Apia.Scope;

/// <summary>
/// Optional backend-specific extension of <see cref="IVaultScope{TRecord,TFilter}"/> that
/// exposes a LINQ predicate so Postgres can push it into a SQL WHERE clause instead of
/// filtering in-process.
/// </summary>
public interface ILinqVaultScope<TRecord, TFilter> : IVaultScope<TRecord, TFilter>
{
    Expression<Func<TRecord, bool>> Filter(TFilter filter);
}
