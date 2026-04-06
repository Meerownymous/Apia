using System.Linq.Expressions;
using Apia.Query;

namespace Apia;

public static partial class FilterExtensions
{
    public static Query<T> OrderByDescending<T, TKey>(this Query<T> query, Expression<Func<T, TKey>> expr)
    {
        query.OrderList.Add(new OrderNode(Query<T>.FieldName(expr), Descending: true));
        return query;
    }
}
