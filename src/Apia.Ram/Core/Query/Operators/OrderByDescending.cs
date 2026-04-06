using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    public static Query<T> OrderByDescending<T, TKey>(this Query<T> query, Expression<Func<T, TKey>> expr)
        => query.AppendOrder(new OrderNode(FieldName(expr), Descending: true));
}
