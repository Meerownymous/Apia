using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    public static IConditionBuilder<T> And<T, TField>(this Query<T> query, Expression<Func<T, TField>> expr)
        => new ConditionBuilder<T>(query, Connector.And, FieldName(expr));
}
