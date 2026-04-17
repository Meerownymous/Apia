using System.Linq.Expressions;
using OneOf;

namespace Apia.Scope;

/// <summary>
/// Describes which records of type <typeparamref name="TRecord"/> are visible and mutable
/// for a given filter value of type <typeparamref name="TFilter"/>.
/// Override <see cref="AsLinq"/> to enable backend-level SQL pushdown.
/// </summary>
public interface IScope<TRecord, TFilter>
{
    bool Includes(TRecord record, TFilter filter);

    bool CanWrite(TRecord record, TFilter filter) => Includes(record, filter);

    bool CanDelete(TRecord record, TFilter filter) => Includes(record, filter);

    /// <summary>
    /// Optional LINQ expression equivalent of <see cref="Includes"/>.
    /// When provided, backends can push this into a SQL WHERE clause instead of filtering in-process.
    /// </summary>
    OneOf<Expression<Func<TRecord, bool>>, None> AsLinq(TFilter filter) => new None();
}
