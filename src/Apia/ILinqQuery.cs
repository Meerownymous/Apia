using System.Linq.Expressions;

namespace Apia;

/// <summary>A query that carries a LINQ predicate for filtering entities of type T.</summary>
public interface ILinqQuery<T>
{
    /// <summary>The filter predicate to apply.</summary>
    Expression<Func<T, bool>> Predicate();
}
