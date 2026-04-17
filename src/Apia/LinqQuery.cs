using System.Linq.Expressions;

namespace Apia;

/// <summary>
/// Query that carries a LINQ predicate. Backends that understand it apply it as a SQL WHERE clause;
/// others compile it to an in-process predicate.
/// </summary>
public sealed record LinqQuery<T>(Expression<Func<T, bool>> Predicate) : IQuery<Expression<Func<T, bool>>>;
