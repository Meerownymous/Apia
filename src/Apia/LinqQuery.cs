using System.Linq.Expressions;

namespace Apia;

/// <summary>
/// A query that carries a LINQ predicate. Backends that understand it apply it as a SQL WHERE clause;
/// others compile it to an in-process predicate.
/// </summary>
public sealed class LinqQuery<T>(Expression<Func<T, bool>> predicate) : IQuery<Expression<Func<T, bool>>>
{
    public Expression<Func<T, bool>> Seed() => predicate;
}
