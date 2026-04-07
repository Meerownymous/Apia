using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    /// <summary>Appends a descending sort on the selected field.</summary>
    public static Query<T> OrderByDescending<T, TValue>(this Query<T> query, Expression<Func<T, TValue>> field)
        => query.AppendOrder(new OrderNode(((MemberExpression)field.Body).Member.Name, Descending: true));
}
